using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class SocketChannelTest
    {
        public static Task<string> TestMethod(string input)
        {
            return Task.FromResult(input + " TEST");
        }
        [Test]
        [Timeout(500)]
        public void PrimitiveString()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen(2);
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)socket.LocalEndPoint).Port));
            var serverSocket = socket.Accept();
            var serverChannel = new SocketChannel(serverSocket, new BinaryGraphSerializer(ReplicationModel.Default));
            var clientChannel = new SocketChannel(clientSocket, new BinaryGraphSerializer(ReplicationModel.Default));
            serverChannel.Respond<string, string>(TestMethod);
            var result = clientChannel.Request(() => TestMethod("derp")).GetAwaiter().GetResult();
            Assert.AreEqual("derp TEST", result);
        }
    }
}
