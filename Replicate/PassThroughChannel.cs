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

            public ushort LocalID { get; set;  }

            public bool IsOpen => true;

            public Task<byte[]> Poll()
            {
                return taskQueue.Poll();
            }

            public void Send(ushort? destination, byte[] message, ReliabilityMode reliability)
            {
                foreach (var endpoint in channel.endpoints.Where(other => (destination == null || destination == other.LocalID) && other != this))
                {
                    endpoint.taskQueue.Put(message);
                }
            }
        }
        List<PassThroughChannelEndpoint> endpoints = new List<PassThroughChannelEndpoint>();
        public IReplicationChannel CreateEndpoint(ushort id)
        {
            var ep = new PassThroughChannelEndpoint() { channel = this, LocalID =  id };
            endpoints.Add(ep);
            return ep;
        }
    }
}
