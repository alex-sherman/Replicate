using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Replicate.Messages;
using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;

namespace Replicate
{
    class CastedSerializer<T> : IReplicateSerializer<object>
    {
        IReplicateSerializer<T> serializer;
        public CastedSerializer(IReplicateSerializer<T> inner)
        {
            serializer = inner;
        }
        public object Deserialize(Type type, object message)
        {
            return serializer.Deserialize(type, (T)message);
        }

        public object Serialize(Type type, object obj)
        {
            return serializer.Serialize(type, obj);
        }
    }
    public class NonSerializer : IReplicateSerializer<object>
    {
        public object Deserialize(Type type, object wireValue) => wireValue;
        public object Serialize(Type type, object obj) => obj;
    }
    public class PassThroughChannel
    {
        public class Endpoint : RPCChannel<string, object>
        {
            public Endpoint target;

            private IReplicateSerializer<object> serializer = new NonSerializer();
            public override IReplicateSerializer<object> Serializer => serializer;
            public void SetSerializer(IReplicateSerializer<object> serializer) => this.serializer = serializer;

            public override string GetEndpoint(MethodInfo method)
            {
                return $"{method.DeclaringType}.{method.Name}";
            }

            public override Task<object> Request(string messageId, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
            {
                if (Serializer == null)
                    return target.Receive(messageId, request);
                return target.Receive(messageId, Serializer.Serialize(request.Contract.RequestType, request.Request), request.Target);
            }
        }

        public Endpoint PointA;
        public Endpoint PointB;
        public PassThroughChannel()
        {
            var pointA = new Endpoint();
            var pointB = new Endpoint() { target = pointA };
            pointA.target = pointB;
            PointA = pointA;
            PointB = pointB;
        }
        public void SetSerializer<T>(IReplicateSerializer<T> serializer)
        {
            PointA.SetSerializer(new CastedSerializer<T>(serializer));
            PointB.SetSerializer(new CastedSerializer<T>(serializer));
        }
    }
}
