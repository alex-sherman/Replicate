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
                client = new ReplicationManager(channel.CreateEndpoint()),
                server = new ReplicationManager(channel.CreateEndpoint())
            };
        }
    }
}
