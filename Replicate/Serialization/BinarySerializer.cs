using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    class IntSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadInt32();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteInt32(Convert.ToInt32(obj));
        }
    }
    class FloatSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadSingle();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteSingle((float)obj);
        }
    }
    class StringSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadString();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteString((string)obj);
        }
    }
    public class BinarySerializer : Serializer
    {
        public BinarySerializer(ReplicationModel model) : base(model)
        {
        }
        static IntSerializer intSer = new IntSerializer();
        Dictionary<Type, IReplicateSerializer> serializers = new Dictionary<Type, IReplicateSerializer>()
        {
            {typeof(byte), intSer },
            {typeof(int), intSer },
            {typeof(uint), intSer },
            {typeof(long), intSer },
            {typeof(ulong), intSer },
            {typeof(string), new StringSerializer() },
            {typeof(float), new FloatSerializer() },
        };
        public override object DeserializePrimitive(Stream stream, Type type, TypeAccessor typeData)
        {
            if (serializers.ContainsKey(type))
                return Convert.ChangeType(serializers[type].Read(stream), type);
            return null;
        }

        public override void SerializePrimitive(Stream stream, object obj, TypeAccessor typeData)
        {
            var type = typeData.Type;
            if (serializers.ContainsKey(type))
                serializers[type].Write(obj, stream);
        }
    }
}
