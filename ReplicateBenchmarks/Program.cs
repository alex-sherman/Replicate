using Replicate;
using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReplicateBenchmarks {
    [ProtoBuf.ProtoContract]
    [ReplicateType]
    public struct Derp {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public string faff;
    }
    [ProtoBuf.ProtoContract]
    [ReplicateType]
    public struct GenericDerp<T> {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public T faff;
    }
    [ProtoBuf.ProtoContract]
    [ReplicateType]
    public struct Collection<T> {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public List<T> List;
    }
    public class Program {
        public interface IEchoService {
            [ReplicateRPC]
            Task<string> Echo(string value);
        }
        public class EchoService : IEchoService {
            public Task<string> Echo(string value) { return Task.FromResult(value); }
        }
        static void TimeSerialize<T>(string name, T value, Action<Stream, T> serialize, Func<Stream, T> deserialize, double count = 1e6) {
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) {
                MemoryStream stream = new MemoryStream();
                serialize(stream, value);
                stream.Position = 0;
                deserialize(stream);
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }
        static void TimeSerialize<T>(string name, T value, IReplicateSerializer serializer, double count = 1e6) {
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) {
                serializer.Deserialize<T>(serializer.Serialize(value));
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }

        static void Main(string[] args) {
            //JsonCompare();
            //BinaryCompare();
            ProtoCompare();
            //ServerTest();
            Console.ReadLine();
        }
        static int N = (int)1e5;
        static int n = N;
        public static void ServerTest() {
            var model = new ReplicationModel();
            model.Add(typeof(IEchoService));
            //Server side
            var server = new RPCServer(model);
            server.WithReflection();
            server.RegisterSingleton<IEchoService>(new EchoService());

            var listenerTask = new SocketListener(server, 55555, new BinarySerializer(model)).Task;
            var clientChannel = SocketChannel.Connect("localhost", 55555, new BinarySerializer(model));
            var echoService = clientChannel.CreateProxy<IEchoService>();
            var s = Stopwatch.StartNew();
            List<Task> RPCs = new List<Task>();
            int maxOutstanding = 100;
            while (n > 0) {
                while (RPCs.Count < maxOutstanding)
                    RPCs.Add(StartRPC(echoService));
                RPCs[0].GetAwaiter().GetResult();
                RPCs.RemoveAt(0);
            }
            s.Stop();
            Console.WriteLine("RPC/s: " + N / (s.ElapsedTicks * 1.0 / Stopwatch.Frequency));
        }
        public static Task StartRPC(IEchoService service) {
            return service.Echo("derp").ContinueWith(t => { Interlocked.Decrement(ref n); });
        }
        static void ProtoCompare() {
            var model = ReplicationModel.Default;
            model.Add(typeof(Derp));
            model.Add(typeof(GenericDerp<>));
            model.Add(typeof(Collection<>));

            var ser = new ProtoSerializer(model);
            var herp = new Derp() { faff = "faff" };
            var derp = "faff";
            var herpList = new Collection<Derp>() { List = new List<Derp> { herp, herp, herp } };
            var genDerp = new GenericDerp<string>() { faff = "faff" };

            //TimeSerialize("Serialize String", derp, ser);
            //TimeSerialize("Proto Serialize String", derp,
            //    ProtoBuf.Serializer.Serialize, ProtoBuf.Serializer.Deserialize<string>);

            TimeSerialize("Serialize Derp", herp, ser, 1e9);
            //TimeSerialize("Proto Serialize Derp", herp,
            //    ProtoBuf.Serializer.Serialize, ProtoBuf.Serializer.Deserialize<Derp>);

            //TimeSerialize("Serialize List<Derp>", herpList, ser, count: 1e5);
            //TimeSerialize("Proto Serialize List<Derp>", herpList,
            //    ProtoBuf.Serializer.Serialize, ProtoBuf.Serializer.Deserialize<Collection<Derp>>, 1e5);

            //TimeSerialize("Serialize GenericDerp<string>", genDerp, ser);
            //TimeSerialize("Proto Serialize GenericDerp<string>", genDerp,
            //    ProtoBuf.Serializer.Serialize, ProtoBuf.Serializer.Deserialize<GenericDerp<string>>);
        }
        static void BinaryCompare() {
            var model = ReplicationModel.Default;
            model.Add(typeof(Derp));
            model.Add(typeof(GenericDerp<>));

            var serGraph = new BinaryGraphSerializer(model);
            var ser = new BinarySerializer(model);
            var herp = new Derp() { faff = "faff" };
            var derp = "faff";
            var herpList = new List<Derp> { herp, herp, herp };
            var genDerp = new GenericDerp<string>() { faff = "faff" };

            TimeSerialize("Serialize String", derp, ser);
            TimeSerialize("Graph Serialize String", derp, serGraph);

            TimeSerialize("Serialize Derp", herp, ser);
            TimeSerialize("Graph Serialize Derp", herp, serGraph);

            TimeSerialize("Serialize List<Derp>", herpList, ser, count: 1e5);
            TimeSerialize("Graph Serialize List<Derp>", herpList, serGraph, count: 1e5);

            TimeSerialize("Serialize GenericDerp<string>", genDerp, ser);
            TimeSerialize("Graph Serialize GenericDerp<string>", genDerp, serGraph);
        }
        static void JsonCompare() {
            var model = ReplicationModel.Default;
            model.Add(typeof(Derp));
            model.Add(typeof(GenericDerp<>));

            var serGraph = new JSONGraphSerializer(model);
            var ser = new JSONSerializer(model);
            var herp = new Derp() { faff = "faff" };
            var derp = "faff";
            var herpList = new List<Derp> { herp, herp, herp };
            var genDerp = new GenericDerp<string>() { faff = "faff" };
            var dict = new Dictionary<string, string>() { { "faff", "faff" } };
            TimeSerialize("Serialize String", derp, ser);
            TimeSerialize("Graph Serialize String", derp, serGraph);

            TimeSerialize("Serialize Derp", herp, ser);
            TimeSerialize("Graph Serialize Derp", herp, serGraph);

            TimeSerialize("Serialize List<Derp>", herpList, ser, count: 1e4);
            TimeSerialize("Graph Serialize List<Derp>", herpList, serGraph, count: 1e4);

            TimeSerialize("Serialize GenericDerp<string>", genDerp, ser);
            TimeSerialize("Graph Serialize GenericDerp<string>", genDerp, serGraph);

            //TimeSerialize("Serialize Dictionary<string, string>", dict, ser);
            //TimeSerialize("Graph Serialize Dictionary<string, string>", dict, serGraph);
        }
    }
}
