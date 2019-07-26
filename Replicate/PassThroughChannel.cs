using System;
using System.Collections.Generic;
using System.IO;
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
    public class NonSerializer : IReplicateSerializer
    {
        class SecretStream : MemoryStream
        {
            public object Obj;
        }
        public object Deserialize(Type type, Stream wireValue, object existing = null) => (wireValue as SecretStream).Obj;
        public T Deserialize<T>(Stream wireValue) => (T)(wireValue as SecretStream).Obj;
        public Stream Serialize(Type type, object obj, Stream _) => new SecretStream() { Obj = obj };
    }
    public class PassThroughChannel
    {
        public class Endpoint : RPCChannel<string, Stream>
        {
            public Endpoint target;

            public Endpoint() : base(new NonSerializer()) { }
            public Endpoint(IReplicateSerializer serializer) : base(serializer) { }

            public override string GetEndpoint(MethodInfo method)
            {
                return $"{method.DeclaringType}.{method.Name}";
            }
            public override Stream GetWireValue(Stream stream) => stream;
            public override Stream GetStream(Stream stream) => stream;

            public override Task<Stream> Request(string messageId, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
            {
                return target.Receive(messageId, Serializer.Serialize(request.Contract.RequestType, request.Request), request.Target);
            }
        }

        public Endpoint PointA;
        public Endpoint PointB;
        public PassThroughChannel()
        {
        }
        public void SetSerializer(IReplicateSerializer serializer)
        {
            var pointA = new Endpoint(serializer);
            var pointB = new Endpoint(serializer) { target = pointA };
            pointA.target = pointB;
            PointA = pointA;
            PointB = pointB;
        }
    }
}
