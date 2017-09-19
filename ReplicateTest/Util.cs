using Replicate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    public static class Util
    {
        public struct ClientServer
        {
            public PassThroughChannel channel;
            public ReplicationManager server;
            public ReplicationManager client;
        }
        public static ClientServer MakeClientServer()
        {
            PassThroughChannel channel = new PassThroughChannel();
            return new ClientServer()
            {
                channel = channel,
                client = new ReplicationManager() { ID = 1 }.RegisterClient(0, channel.CreateEndpoint()),
                server = new ReplicationManager() { ID = 0 }.RegisterClient(1, channel.CreateEndpoint())
            };
        }
    }
}
