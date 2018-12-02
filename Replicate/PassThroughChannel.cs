using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Replicate.Messages;
using Replicate.Serialization;

namespace Replicate
{
    public class PassThroughChannel
    {

        class PassThroughSerializer : IReplicateSerializer<object>
        {
            public object Deserialize(Type type, object message) => message;
            public object Serialize(Type type, object obj) => obj;
        }
        class PassThroughChannelEndpoint : ReplicationChannel<string, object>
        {
            public PassThroughChannelEndpoint target;

            public override IReplicateSerializer<object> Serializer { get; } = new PassThroughSerializer();

            public override string GetEndpoint(MethodInfo method)
            {
                return $"{method.DeclaringType}.{method.Name}";
            }

            public override Task<object> Request(string messageID, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
            {
                return target.Receive(messageID, request);
            }
        }

        public ReplicationChannel<string, object> PointA;
        public ReplicationChannel<string, object> PointB;
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
