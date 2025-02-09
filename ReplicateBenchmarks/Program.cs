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
    public class Program {
        public interface IEchoService {
            [ReplicateRPC]
            Task<string> Echo(string value);
        }
        public class EchoService : IEchoService {
            public Task<string> Echo(string value) { return Task.FromResult(value); }
        }
        static MemoryStream stream = new MemoryStream((int)1e8);
        static void TimeSerialize<T>(string name, T value, Action<Stream, T> serialize, double count = 1e6) {
            stream.Seek(0, SeekOrigin.Begin);
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) {
                serialize(stream, value);
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }
        static void TimeSerialize<T>(string name, T value, IReplicateSerializer serializer, double count = 1e6) {
            stream.Seek(0, SeekOrigin.Begin);
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) {
                serializer.Serialize(value);
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }

        static void Main(string[] args) {
            //JsonCompare();
            BinaryCompare();
            //ProtoCompare();
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

            var ser = new BinarySerializer(model);
            var herp = new Derp() { faff = "faff" };
            var derp = "faff";
            var herpList = new List<Derp> { herp, herp, herp };
            var genDerp = new GenericDerp<string>() { faff = "faff" };
            //ser.Serialize(new MemoryStream(), herp);
            TimeSerialize("Serialize String", derp, ser);
            TimeSerialize("Proto Serialize String", derp, ProtoBuf.Serializer.Serialize);

            TimeSerialize("Serialize Derp", herp, ser);
            TimeSerialize("Proto Serialize Derp", herp, ProtoBuf.Serializer.Serialize);

            TimeSerialize("Serialize List<Derp>", herpList, ser, count: 1e4);
            TimeSerialize("Proto Serialize List<Derp>", herpList, ProtoBuf.Serializer.Serialize, count: 1e4);

            TimeSerialize("Serialize GenericDerp<string>", genDerp, ser);
            TimeSerialize("Proto Serialize GenericDerp<string>", genDerp, ProtoBuf.Serializer.Serialize);
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
