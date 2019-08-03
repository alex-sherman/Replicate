﻿using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [Flags]
    public enum MessageFlags
    {
        None,
        Request = 1,
        Response = 2,
    }
    struct MessageHeader
    {
        public MessageFlags Flags;
        public uint Sequence;
        public int Length;
        public const int SIZE = 9;
        public static implicit operator byte[] (MessageHeader header)
        {
            var output = new byte[SIZE];
            output[0] = (byte)header.Flags;
            BitConverter.GetBytes(header.Sequence).CopyTo(output, 1);
            BitConverter.GetBytes(header.Length).CopyTo(output, 5);
            return output;
        }
        public static implicit operator MessageHeader(byte[] bytes)
        {
            return new MessageHeader()
            {
                Flags = (MessageFlags)bytes[0],
                Sequence = BitConverter.ToUInt32(bytes, 1),
                Length = BitConverter.ToInt32(bytes, 5),
            };
        }
    }
    public class SocketChannel : RPCChannel<MemoryStream>
    {
        const int BUFFERSIZE = 1024;
        private byte[] buffer = new byte[BUFFERSIZE];
        private MessageHeader currentHeader;
        private int remainingBytes;
        private MemoryStream currentMessage = null;
        private Dictionary<uint, TaskCompletionSource<Stream>> outStandingMessages = new Dictionary<uint, TaskCompletionSource<Stream>>();
        uint sequence = 0xFAFF;
        public readonly Socket Socket;
        private TcpClient client;

        public static SocketChannel Connect(string host, int port, IReplicateSerializer serializer)
        {
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(host, port);
            var channel = new SocketChannel(clientSocket, serializer);
            channel.Start();
            return channel;
        }

        public static Task Listen(IRPCServer server, int port, IReplicateSerializer serializer) => Listen(server, null, port, serializer);
        public static Task Listen(IRPCServer server, string host, int port, IReplicateSerializer serializer)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(host == null ? IPAddress.Any : IPAddress.Parse(host), port));
            socket.Listen(16);
            return Task.Run(() =>
            {
                while (true)
                {
                    var client = socket.Accept();
                    new SocketChannel(client, serializer) { Server = server }.Start();
                }
            });
        }

        public SocketChannel(Socket socket, IReplicateSerializer serializer)
            : base(serializer)
        {
            Socket = socket;
        }

        public void Start() { Socket.BeginReceive(buffer, 0, MessageHeader.SIZE, SocketFlags.None, startMessage, null); }
        public void Close() { Socket.Close(); }

        private void startMessage(IAsyncResult result)
        {
            try
            {
                var bytes = Socket.EndReceive(result);
                Debug.Assert(bytes == 9);
                currentHeader = buffer;
                remainingBytes = currentHeader.Length;
                currentMessage = new MemoryStream(remainingBytes);
                Socket.BeginReceive(buffer, 0, Math.Min(BUFFERSIZE, remainingBytes), SocketFlags.None, continueMessage, null);
            }
            catch (SocketException)
            {
                Socket.Close();
            }
        }

        private void continueMessage(IAsyncResult result)
        {
            try
            {
                var bytes = Socket.EndReceive(result);
                currentMessage.Write(buffer, 0, bytes);
                remainingBytes -= bytes;
                if (remainingBytes == 0) finishMessage();
                else Socket.BeginReceive(buffer, 0, Math.Min(BUFFERSIZE, remainingBytes), SocketFlags.None, continueMessage, null);
            }
            catch (SocketException)
            {
                Socket.Close();
            }
        }

        private void finishMessage()
        {
            currentMessage.Position = 0;
            if (currentHeader.Flags.HasFlag(MessageFlags.Response))
            {
                outStandingMessages[currentHeader.Sequence].SetResult(currentMessage);
                outStandingMessages.Remove(currentHeader.Sequence);
            }
            else if (currentHeader.Flags.HasFlag(MessageFlags.Request))
            {
                var endpoint = Serializer.Deserialize<MethodKey>(currentMessage);
                // currentMessage.Position _should_ be updated here, will break if two calls to deserialize happen
                var recvTask = Receive(endpoint, currentMessage);
                recvTask.ContinueWith(task => SendResponse(currentHeader.Sequence, task.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            currentMessage = null;
            remainingBytes = 0;
            Socket.BeginReceive(buffer, 0, MessageHeader.SIZE, SocketFlags.None, startMessage, null);
        }

        protected void SendResponse(uint sequence, MemoryStream response)
        {
            var message = response.ToArray();
            var header = new MessageHeader() { Flags = MessageFlags.Response, Length = message.Length, Sequence = sequence };
            lock (this)
            {
                Socket.Send(header);
                Socket.Send(message);
            }
        }

        public override Task<Stream> Request(RPCRequest request, ReliabilityMode reliability)
        {
            var messageSequence = sequence++;
            var messageStream = new MemoryStream();
            Serializer.Serialize(request.Endpoint, messageStream);
            Serializer.Serialize(request.Contract.RequestType, request.Request, messageStream);
            var header = new MessageHeader() { Flags = MessageFlags.Request, Length = (int)messageStream.Length, Sequence = messageSequence };
            lock (this)
            {
                Socket.Send(header);
                Socket.Send(messageStream.ToArray());
            }
            return (outStandingMessages[messageSequence] = new TaskCompletionSource<Stream>()).Task;
        }

        public override Stream GetStream(MemoryStream wireValue) => wireValue;

        public override MemoryStream GetWireValue(Stream stream)
        {
            var output = new MemoryStream();
            stream.CopyTo(output);
            return output;
        }
    }
}
