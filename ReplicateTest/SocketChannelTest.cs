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
    [ReplicateType]
    public interface IEchoService
    {
        Task<string> Echo(string message);
    }
    [ReplicateType]
    [TestFixture]
    public class SocketChannelTest
    {
        [ReplicateRPC]
        public static Task<string> TestMethod(string input)
        {
            return Task.FromResult(input + " TEST");
        }
        [Test]
        [Timeout(1000)]
        public async Task PrimitiveString()
        {
            var model = new ReplicationModel();
            // Server side
            var server = new RPCServer(model);
            server.Respond<string, string>(TestMethod);
            using (new SocketListener(server, 55554, new BinarySerializer(model)))
            {
                var clientChannel = SocketChannel.Connect("127.0.0.1", 55554, new BinarySerializer(model));
                var result = await clientChannel.Request(() => TestMethod("derp"));
                Assert.AreEqual("derp TEST", result);
            }
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
            var model = new ReplicationModel();
            //Server side
            var server = new RPCServer(model);
            server.RegisterSingleton<IEchoService>(new EchoService());
            using (new SocketListener(server, 55555, new BinarySerializer(model)))
            {
                //Client side
                var clientChannel = SocketChannel.Connect("localhost", 55555, new BinarySerializer(model));
                var echoService = clientChannel.CreateProxy<IEchoService>();
                Assert.AreEqual("Hello! DONE", await echoService.Echo("Hello!"));
            }
        }
    }
}
