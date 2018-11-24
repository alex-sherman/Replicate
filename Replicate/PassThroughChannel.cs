using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Replicate.Messages;

namespace Replicate
{
    public class PassThroughChannel
    {
        class PassThroughChannelEndpoint : ReplicationChannel<string>
        {
            public PassThroughChannelEndpoint target;

            public override string GetEndpoint(MethodInfo method)
            {
                return $"{method.DeclaringType}.{method.Name}";
            }

            public override Task<object> Publish(string messageID, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
            {
                return target.Receive(messageID, request);
            }
        }

        public ReplicationChannel<string> PointA;
        public ReplicationChannel<string> PointB;
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
