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

            public Endpoint() : base(new NonSerializer()) { }
            public Endpoint(IReplicateSerializer<object> serializer) : base(serializer) { }

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
        }
        public void SetSerializer<T>(IReplicateSerializer<T> serializer)
        {
            var objectSer = new CastedSerializer<T>(serializer);
            var pointA = new Endpoint(objectSer);
            var pointB = new Endpoint(objectSer) { target = pointA };
            pointA.target = pointB;
            PointA = pointA;
            PointB = pointB;
        }
    }
}
