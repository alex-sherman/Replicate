using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public class PassThroughChannel
    {
        class PassThroughChannelEndpoint : ReplicationChannel
        {
            public PassThroughChannel channel;
            public bool IsOpen => true;

            public PassThroughChannelEndpoint(ushort localID)
            {
                this.LocalID = localID;
            }

            public override void Send(ushort? destination, byte[] message, ReliabilityMode reliability)
            {
                foreach (var endpoint in channel.endpoints.Where(other => (destination == null || destination == other.LocalID) && other != this))
                {
                    endpoint.Put(message);
                }
            }
        }
        List<PassThroughChannelEndpoint> endpoints = new List<PassThroughChannelEndpoint>();
        public ReplicationChannel CreateEndpoint(ushort id)
        {
            var ep = new PassThroughChannelEndpoint(id) { channel = this };
            endpoints.Add(ep);
            return ep;
        }
    }
}
