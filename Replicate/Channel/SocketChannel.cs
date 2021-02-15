using Replicate.MetaData;
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
        Error = 4,
    }
    struct MessageHeader
    {
        public MessageFlags Flags;
        public uint Sequence;
        public int Length;
        public const int SIZE = 9;
        public static implicit operator byte[](MessageHeader header)
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
        private byte[] headerBuffer = new byte[MessageHeader.SIZE];
        private MessageHeader currentHeader;
        private int remainingBytes;
        private byte[] currentMessage;
        private Dictionary<uint, TaskCompletionSource<Stream>> outStandingMessages = new Dictionary<uint, TaskCompletionSource<Stream>>();
        uint sequence = 0xFAFF;
        public readonly Socket Socket;
        private volatile List<ArraySegment<byte>> sendQueue = new List<ArraySegment<byte>>();
        private volatile bool sending = false;
        private volatile SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        private volatile SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();

        public static SocketChannel Connect(string host, int port, IReplicateSerializer serializer)
        {
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(host, port);
            clientSocket.Blocking = false;
            var channel = new SocketChannel(clientSocket, serializer);
            channel.Start();
            return channel;
        }

        internal SocketChannel(Socket socket, IReplicateSerializer serializer)
            : base(serializer)
        {
            Socket = socket;
            sendArgs.Completed += OnSend;
            recvArgs.Completed += OnRecv;
        }
        private void Send(params byte[][] bytes)
        {
            lock (sendArgs)
            {
                foreach (var payload in bytes)
                {
                    sendQueue.Add(new ArraySegment<byte>(payload));
                }
                if (!sending)
                {
                    BeginSend();
                }
            }
        }
        private void BeginSend()
        {
            lock (sendArgs)
            {
                sending = true;
                sendArgs.BufferList = sendQueue;
                sendQueue = new List<ArraySegment<byte>>();
                if (!Socket.SendAsync(sendArgs)) throw new Exception();
            }
        }
        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            lock (sendArgs)
            {
                if (sendQueue.Count != 0) BeginSend();
                else sending = false;
            }
        }

        public void Start()
        {
            recvArgs.SetBuffer(headerBuffer, 0, MessageHeader.SIZE);
            if (!Socket.ReceiveAsync(recvArgs)) OnRecv(this, recvArgs);
        }
        public void Close() { Socket.Close(); }

        private void OnRecv(object source, SocketAsyncEventArgs args)
        {
            try
            {
                do
                {
                    if (currentMessage == null)
                    {
                        if (args.BytesTransferred != MessageHeader.SIZE) throw new SocketException();
                        currentHeader = headerBuffer;
                        remainingBytes = currentHeader.Length;
                        currentMessage = new byte[remainingBytes];
                        recvArgs.SetBuffer(currentMessage, 0, remainingBytes);
                        if (Socket.ReceiveAsync(recvArgs)) return;
                    }
                    remainingBytes -= recvArgs.BytesTransferred;
                    if (remainingBytes == 0) FinishMessage();
                } while (!Socket.ReceiveAsync(recvArgs));
            }
            catch (SocketException)
            {
                Socket.Close();
            }
        }

        private void FinishMessage()
        {
            var messageStream = new MemoryStream(currentMessage);
            if (currentHeader.Flags.HasFlag(MessageFlags.Response))
            {
                HandleResponse(currentHeader, messageStream);
            }
            else if (currentHeader.Flags.HasFlag(MessageFlags.Request))
            {
                var endpoint = Serializer.Deserialize<MethodKey>(messageStream);
                // currentMessage.Position _should_ be updated here, will break if two calls to deserialize happen
                HandleRequest(endpoint, messageStream, currentHeader.Sequence).ConfigureAwait(false);
            }
            currentMessage = null;
            remainingBytes = 0;
            recvArgs.SetBuffer(headerBuffer, 0, MessageHeader.SIZE);
        }

        private void HandleResponse(MessageHeader header, MemoryStream currentMessage)
        {
            TaskCompletionSource<Stream> source;
            lock (this)
            {
                source = outStandingMessages[header.Sequence];
                outStandingMessages.Remove(header.Sequence);
            }
            if (header.Flags.HasFlag(MessageFlags.Error))
            {
                var message = Serializer.Deserialize<string>(currentMessage);
                source.SetException(new ReplicateError(message));
            }
            else
                source.SetResult(currentMessage);
        }

        private Task HandleRequest(MethodKey endpoint, MemoryStream currentMessage, uint sequence)
        {
            return Receive(endpoint, currentMessage).ContinueWith(t =>
            {
                var header = new MessageHeader()
                {
                    Flags = MessageFlags.Response,
                    Sequence = sequence,
                };
                byte[] message;
                try
                {
                    var result = t.Result;
                    message = result.ToArray();
                }
                catch (Exception e)
                {
                    header.Flags |= MessageFlags.Error;
                    var errorStream = new MemoryStream();
                    string errorMessage = e is AggregateException a ? a.InnerExceptions[0].Message : e.Message;
                    Serializer.Serialize(typeof(string), errorMessage, errorStream);
                    message = errorStream.ToArray();
                }
                header.Length = message.Length;
                Send(header, message);
            });
        }

        public override Task<Stream> Request(RPCRequest request, ReliabilityMode reliability)
        {
            var messageSequence = sequence++;
            var messageStream = new MemoryStream();
            Serializer.Serialize(request.Endpoint, messageStream);
            Serializer.Serialize(request.Contract.RequestType, request.Request, messageStream);
            var header = new MessageHeader() { Flags = MessageFlags.Request, Length = (int)messageStream.Length, Sequence = messageSequence };
            Task<Stream> result;
            lock (this)
            {
                result = (outStandingMessages[messageSequence] = new TaskCompletionSource<Stream>()).Task;
            }
            Send(header, messageStream.ToArray());
            return result;
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
