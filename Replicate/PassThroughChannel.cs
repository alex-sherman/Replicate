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
            public PassThroughChannelEndpoint target;

            public override string GetMessageID(Type type)
            {
                return type.Name;
            }

            public override Task<TResponse> Publish<TRequest, TResponse>(TRequest request, ReliabilityMode reliability)
            {
                return target.Receive<TRequest, TResponse>(request);
            }
        }

        public IReplicationChannel PointA;
        public IReplicationChannel PointB;
        public PassThroughChannel()
        {
            var pointA = new PassThroughChannelEndpoint();
            var pointB = new PassThroughChannelEndpoint() { target = pointA };
            pointA.target = pointB;
            PointA = pointA;
            PointB = pointB;
        }
    }
}
