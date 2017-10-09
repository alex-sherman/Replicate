using Replicate;
using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    public static class Util
    {
        public struct ClientServer
        {
            public ReplicationModel model;
            public PassThroughChannel channel;
            public ReplicationManager server;
            public ReplicationManager client;
        }
        public static ClientServer MakeClientServer()
        {
            ReplicationModel model = new ReplicationModel();
            PassThroughChannel channel = new PassThroughChannel();
            return new ClientServer()
            {
                model = model,
                channel = channel,
                client = new ReplicationManager(model) { ID = 1 }.RegisterClient(0, channel.CreateEndpoint()),
                server = new ReplicationManager(model) { ID = 0 }.RegisterClient(1, channel.CreateEndpoint())
            };
        }
    }
}
