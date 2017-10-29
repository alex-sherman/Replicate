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
    [Replicate]
    public struct Derp
    {
        [ProtoBuf.ProtoMember(1)]
        [Replicate]
        public string faff;
    }
    [ProtoBuf.ProtoContract]
    [Replicate]
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
        static void Main(string[] args)
        {
            var model = ReplicationModel.Default;
            model.Add(typeof(Derp));

            Serializer ser = new BinarySerializer(model);
            var herp = new Derp() { faff = "faff" };
            var derp = "faff";
            var herpList = new List<Derp> { herp, herp, herp };
            var genDerp = new GenericDerp<string>() { faff = "faff" };
            ser.Serialize(new MemoryStream(), herp);
            TimeSerialize("Serialize String", derp, ser.Serialize);
            TimeSerialize("Proto Serialize String", derp, ProtoBuf.Serializer.Serialize);
            
            TimeSerialize<object>("Serialize Derp", herp, ser.Serialize);
            TimeSerialize("Proto Serialize Derp", herp, ProtoBuf.Serializer.Serialize);

            TimeSerialize<object>("Serialize List<Derp>", herpList, ser.Serialize, count: 1e4);
            TimeSerialize("Proto Serialize List<Derp>", herpList, ProtoBuf.Serializer.Serialize, count: 1e4);

            TimeSerialize<object>("Serialize GenericDerp<string>", genDerp, ser.Serialize);
            TimeSerialize("Proto Serialize GenericDerp<string>", genDerp, ProtoBuf.Serializer.Serialize);
            Console.ReadLine();
        }
    }
}
