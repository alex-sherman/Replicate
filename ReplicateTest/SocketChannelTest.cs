using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.RPC;
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
        [Timeout(1000)]
        public async Task PrimitiveString()
        {
            // Server side
            var server = new RPCServer();
            server.Respond<string, string>(TestMethod);
            SocketChannel.Listen(server, 55554, new BinarySerializer());
            var clientChannel = SocketChannel.Connect("127.0.0.1", 55554, new BinarySerializer());
            var result = await clientChannel.Request(() => TestMethod("derp"));
            Assert.AreEqual("derp TEST", result);
        }
        [ReplicateType]
        public interface IEchoService
        {
            Task<string> Echo(string message);
        }
        public class EchoService : IEchoService
        {
            public async Task<string> Echo(string message)
            {
                await Task.Delay(100); // Do some work
                return message + " DONE";
            }
        }
        [Test]
        [Timeout(1000)]
        public async Task EchoExample()
        {
            // Server side
            var server = new RPCServer();
            server.RegisterSingleton<IEchoService>(new EchoService());
            SocketChannel.Listen(server, 55555, new BinarySerializer());
            // Client side
            var clientChannel = SocketChannel.Connect("127.0.0.1", 55555, new BinarySerializer());
            var echoService = clientChannel.CreateProxy<IEchoService>();
            Assert.AreEqual("Hello! DONE", await echoService.Echo("Hello!"));
        }
    }
}
