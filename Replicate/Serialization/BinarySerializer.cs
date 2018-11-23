using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    class BinaryIntSerializer : IReplicateSerializer
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
    class BinaryFloatSerializer : IReplicateSerializer
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
    class BinaryStringSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadNString();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteNString((string)obj);
        }
    }
    public class BinarySerializer : Serializer
    {
        public BinarySerializer(ReplicationModel model) : base(model) { }
        static BinaryIntSerializer intSer = new BinaryIntSerializer();
        Dictionary<Type, IReplicateSerializer> serializers = new Dictionary<Type, IReplicateSerializer>()
        {
            {typeof(byte), intSer },
            {typeof(short), intSer },
            {typeof(ushort), intSer },
            {typeof(int), intSer },
            {typeof(uint), intSer },
            {typeof(long), intSer },
            {typeof(ulong), intSer },
            {typeof(string), new BinaryStringSerializer() },
            {typeof(float), new BinaryFloatSerializer() },
        };

        public override void SerializePrimitive(Stream stream, object obj, Type type)
        {
            if (obj == null)
                stream.WriteByte(0);
            else
            {
                stream.WriteByte(1);
                serializers[type].Write(obj, stream);
            }
        }

        public override void SerializeCollection(Stream stream, object obj, TypeAccessor collectionValueType)
        {
            var count = 0;
            if (obj == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            var enumerable = (IEnumerable)obj;
            foreach (var item in enumerable)
                count++;
            stream.WriteInt32(count);
            foreach (var item in (IEnumerable)obj)
                Serialize(stream, item, collectionValueType, null);
        }

        public override void SerializeObject(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            if (obj == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            stream.WriteInt32(typeAccessor.MemberAccessors.Length);
            for (int id = 0; id < typeAccessor.MemberAccessors.Length; id++)
            {
                var member = typeAccessor.MemberAccessors[id];
                stream.WriteByte((byte)id);
                Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
            }
        }

        public override void SerializeTuple(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            foreach (var member in typeAccessor.MemberAccessors)
                Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
        }


        public override object DeserializePrimitive(Stream stream, Type type)
        {
            var isNull = stream.ReadByte();
            if (isNull == 0) return null;
            if (serializers.ContainsKey(type))
                return Convert.ChangeType(serializers[type].Read(stream), type);
            return null;
        }

        public override object DeserializeObject(object obj, Stream stream, Type type, TypeAccessor typeAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            if (obj == null)
                obj = Activator.CreateInstance(type);
            for (int i = 0; i < count; i++)
            {
                int id = stream.ReadByte();
                var member = typeAccessor.MemberAccessors[id];
                member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.TypeAccessor, member));
            }
            return obj;
        }

        public override object DeserializeCollection(object obj, Stream stream, Type type, TypeAccessor collectionValueAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            return FillCollection(obj, type, Enumerable.Range(0, count)
                .Select(i => Deserialize(null, stream, collectionValueAccessor, null))
                .ToList());
        }

        public override object DeserializeTuple(Stream stream, Type type, TypeAccessor typeAccessor)
        {
            List<object> parameters = new List<object>();
            List<Type> paramTypes = new List<Type>();
            foreach (var member in typeAccessor.MemberAccessors)
            {
                paramTypes.Add(member.Type);
                parameters.Add(Deserialize(null, stream, member.TypeAccessor, member));
            }
            return type.GetConstructor(paramTypes.ToArray()).Invoke(parameters.ToArray());
        }
    }
}
