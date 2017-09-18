using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public class PassThroughChannel
    {
        class PassThroughChannelEndpoint : IReplicationChannel
        {
            public PassThroughChannel channel;
            public TaskQueue<byte[]> taskQueue = new TaskQueue<byte[]>();
            public Task<byte[]> Poll()
            {
                return taskQueue.Poll();
            }

            public void Send(byte[] message, ReliabilityMode reliability)
            {
                foreach (var endpoint in channel.endpoints.Where(other => other != this))
                {
                    endpoint.taskQueue.Put(message);
                }
            }
        }
        List<PassThroughChannelEndpoint> endpoints = new List<PassThroughChannelEndpoint>();
        public IReplicationChannel CreateEndpoint()
        {
            var ep = new PassThroughChannelEndpoint() { channel = this };
            endpoints.Add(ep);
            return ep;
        }
    }
}
