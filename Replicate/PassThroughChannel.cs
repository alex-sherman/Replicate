using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public class PassThroughChannel
    {
        class PassThroughChannelEndpoint : ReplicationChannel<string>
        {
            public PassThroughChannel channel;

            public string GetMessageID(MethodInfo method)
            {
                throw new NotImplementedException();
            }

            public override string GetMessageID(Type type)
            {
                return type.Name;
            }

            public override Task<TResponse> Publish<TRequest, TResponse>(TRequest request, ReliabilityMode reliability)
            {
                foreach (var endpoint in channel.endpoints.Where(other => other != this))
                {
                    endpoint.Receive<TRequest, TResponse>(request);
                }
                return Task.FromResult(default(TResponse));
            }

            public void Subscribe<TRequest>(string messageID, Action<TRequest> handler)
            {
                throw new NotImplementedException();
            }
        }
        List<PassThroughChannelEndpoint> endpoints = new List<PassThroughChannelEndpoint>();
        public IReplicationChannel CreateEndpoint(ushort id)
        {
            var ep = new PassThroughChannelEndpoint() { channel = this };
            endpoints.Add(ep);
            return ep;
        }
    }
}
