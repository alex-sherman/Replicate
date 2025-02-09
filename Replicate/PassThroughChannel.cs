using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Replicate {
    public class NonSerializer : IReplicateSerializer {
        public ReplicationModel Model { get; }
        public NonSerializer(ReplicationModel model) { Model = model; }
        class SecretStream : MemoryStream {
            public object Obj;
        }
        public object Deserialize(Type type, Stream wireValue, object existing = null) => (wireValue as SecretStream).Obj;
        public T Deserialize<T>(Stream wireValue) => (T)(wireValue as SecretStream).Obj;
        public Stream Serialize(Type type, object obj, Stream _) => new SecretStream() { Obj = obj };
    }
    public class PassThroughChannel {
        public class Endpoint : RPCChannel<Stream> {
            public Endpoint target;

            public Endpoint(ReplicationModel model) : this(new NonSerializer(model)) { }
            public Endpoint(IReplicateSerializer serializer) : base(serializer) { Server = new RPCServer(serializer.Model); }

            public override Stream GetWireValue(Stream stream) => stream;
            public override Stream GetStream(Stream stream) => stream;

            public override Task<Stream> Request(RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced) {
                return target.Receive(request.Endpoint, Serializer.Serialize(request.Contract.RequestType, request.Request), request.Target);
            }
        }

        public Endpoint PointA;
        public Endpoint PointB;
        public PassThroughChannel() {
        }
        public void SetSerializer(IReplicateSerializer serializer) {
            var pointA = new Endpoint(serializer);
            var pointB = new Endpoint(serializer) { target = pointA };
            pointA.target = pointB;
            PointA = pointA;
            PointB = pointB;
        }
    }
}
