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

        public SocketChannel(Socket socket, IReplicateSerializer serializer)
            : base(serializer)
        {
            Socket = socket;
        }

        public void Start() { Socket.BeginReceive(buffer, 0, MessageHeader.SIZE, SocketFlags.None, StartMessage, null); }
        public void Close() { Socket.Close(); }

        private void StartMessage(IAsyncResult result)
        {
            try
            {
                var bytes = Socket.EndReceive(result);
                if (bytes != 9) throw new SocketException();
                currentHeader = buffer;
                remainingBytes = currentHeader.Length;
                currentMessage = new MemoryStream(remainingBytes);
                Socket.BeginReceive(buffer, 0, Math.Min(BUFFERSIZE, remainingBytes), SocketFlags.None, ContinueMessage, null);
            }
            catch (SocketException)
            {
                Socket.Close();
            }
        }

        private void ContinueMessage(IAsyncResult result)
        {
            try
            {
                var bytes = Socket.EndReceive(result);
                currentMessage.Write(buffer, 0, bytes);
                remainingBytes -= bytes;
                if (remainingBytes == 0) FinishMessage();
                else Socket.BeginReceive(buffer, 0, Math.Min(BUFFERSIZE, remainingBytes), SocketFlags.None, ContinueMessage, null);
            }
            catch (SocketException)
            {
                Socket.Close();
            }
        }

        private void FinishMessage()
        {
            currentMessage.Position = 0;
            if (currentHeader.Flags.HasFlag(MessageFlags.Response))
            {
                HandleResponse(currentHeader, currentMessage);
            }
            else if (currentHeader.Flags.HasFlag(MessageFlags.Request))
            {
                var endpoint = Serializer.Deserialize<MethodKey>(currentMessage);
                // currentMessage.Position _should_ be updated here, will break if two calls to deserialize happen
                HandleRequest(endpoint, currentMessage).ConfigureAwait(false);
            }
            currentMessage = null;
            remainingBytes = 0;
            Socket.BeginReceive(buffer, 0, MessageHeader.SIZE, SocketFlags.None, StartMessage, null);
        }

        private void HandleResponse(MessageHeader header, MemoryStream currentMessage)
        {
            if (header.Flags.HasFlag(MessageFlags.Error))
            {
                var message = Serializer.Deserialize<string>(currentMessage);
                outStandingMessages[header.Sequence].SetException(new ReplicateError(message));
            }
            else
                outStandingMessages[header.Sequence].SetResult(currentMessage);
            outStandingMessages.Remove(header.Sequence);
        }

        private async Task HandleRequest(MethodKey endpoint, MemoryStream currentMessage)
        {
            var header = new MessageHeader()
            {
                Flags = MessageFlags.Response,
                Sequence = sequence
            };
            byte[] message;
            try
            {
                var result = await Receive(endpoint, currentMessage);
                message = result.ToArray();
            }
            catch (Exception e)
            {
                header.Flags |= MessageFlags.Error;
                var errorStream = new MemoryStream();
                Serializer.Serialize(typeof(string), e.Message, errorStream);
                message = errorStream.ToArray();
            }
            header.Length = message.Length;
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
