using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateBenchmarks
{
    [ProtoBuf.ProtoContract]
    [ReplicateType]
    public struct Derp
    {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public string faff;
    }
    [ProtoBuf.ProtoContract]
    [ReplicateType]
    public struct GenericDerp<T>
    {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public T faff;
    }
    class Program
    {
        static MemoryStream stream = new MemoryStream((int)1e8);
        static void TimeSerialize<T>(string name, T value, Action<Stream, T> serialize, double count = 1e6)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                serialize(stream, value);
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }
        static void TimeSerialize<T, W>(string name, T value, IReplicateSerializer<W> serializer, double count = 1e6)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var s = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                serializer.Serialize(value);
            }
            s.Stop();
            Console.WriteLine(name + ": " + s.ElapsedTicks * 1.0 / Stopwatch.Frequency);
        }

        static void Main(string[] args)
        {
            //JsonCompare();
            BinaryCompare();
            //ProtoCompare();
            Console.ReadLine();
        }
        static void ProtoCompare()
        {
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
        static void BinaryCompare()
        {
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
        static void JsonCompare()
        {
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
