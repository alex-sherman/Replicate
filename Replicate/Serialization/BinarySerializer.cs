using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replicate.Messages;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    public class BinaryIntSerializer : ITypedSerializer
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
    public class BinaryFloatSerializer : ITypedSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadSingle();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteSingle(Convert.ToSingle(obj));
        }
    }
    public class BinaryStringSerializer : ITypedSerializer
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
    public class BinarySerializer : Serializer<MemoryStream, byte[]>
    {
        public BinarySerializer(ReplicationModel model) : base(model) { }
        static BinaryIntSerializer intSer = new BinaryIntSerializer();
        static Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.Byte, intSer },
            {PrimitiveType.Bool, intSer },
            {PrimitiveType.String, new BinaryStringSerializer() },
            {PrimitiveType.Double, new BinaryFloatSerializer() },
            {PrimitiveType.Float, new BinaryFloatSerializer() },
        };
        //Dictionary<Type, ITypedSerializer> serializers = new Dictionary<Type, ITypedSerializer>()
        //{
        //    {typeof(bool), intSer },
        //    {typeof(byte), intSer },
        //    {typeof(short), intSer },
        //    {typeof(ushort), intSer },
        //    {typeof(int), intSer },
        //    {typeof(uint), intSer },
        //    {typeof(long), intSer },
        //    {typeof(ulong), intSer },
        //    {typeof(string), new BinaryStringSerializer() },
        //    {typeof(float), new BinaryFloatSerializer() },
        //};

        public override void SerializePrimitive(MemoryStream stream, object obj, TypeAccessor type)
        {
            if (obj == null)
                stream.WriteByte(0);
            else
            {
                stream.WriteByte(1);
                if (serializers.TryGetValue(type.TypeData.PrimitiveType, out var ser))
                    ser.Write(obj, stream);
                else
                    throw new SerializationError();
            }
        }

        public override void SerializeCollection(MemoryStream stream, object obj, TypeAccessor collectionValueType)
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

        void WriteKey(Stream stream, MemberKey key)
        {
            if (key.Index.HasValue)
                stream.WriteByte((byte)key.Index.Value);
            else if (key.Name != null)
            {
                stream.WriteByte(254);
                serializers[PrimitiveType.String].Write(key.Name, stream);
            }
            else stream.WriteByte(255);
        }
        MemberKey ReadKey(Stream stream)
        {
            var b = (byte)stream.ReadByte();
            if (b == 255) return default(MemberKey);
            if (b != 254) return b;
            return (string)serializers[PrimitiveType.String].Read(stream);
        }

        public override void SerializeObject(MemoryStream stream, object obj, TypeAccessor typeAccessor)
        {
            if (obj == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            int count = 0;
            var countPos = stream.Position; stream.Position += 4;
            foreach (var key in typeAccessor.Keys)
            {
                WriteKey(stream, key);
                var member = typeAccessor[key];
                Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
                count++;
            }
            var endPos = stream.Position; stream.Position = countPos;
            stream.WriteInt32(count); stream.Position = endPos;
        }

        public override void SerializeTuple(MemoryStream stream, object obj, TypeAccessor typeAccessor)
        {
            foreach (var member in typeAccessor.MemberAccessors)
                Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
        }


        public override object DeserializePrimitive(MemoryStream stream, TypeAccessor typeAccessor)
        {
            var isNull = stream.ReadByte();
            if (isNull == 0) return null;
            return typeAccessor.Coerce((serializers[typeAccessor.TypeData.PrimitiveType].Read(stream)));
        }

        public override object DeserializeObject(object obj, MemoryStream stream, TypeAccessor typeAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            if (obj == null)
                obj = typeAccessor.Construct();
            for (int i = 0; i < count; i++)
            {
                int id = stream.ReadByte();
                var member = typeAccessor.MemberAccessors[id];
                member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.TypeAccessor, member));
            }
            return obj;
        }

        public override object DeserializeCollection(object obj, MemoryStream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            return CollectionUtil.FillCollection(obj, typeAccessor.Type, Enumerable.Range(0, count)
                .Select(i => Deserialize(null, stream, collectionValueAccessor, null))
                .ToList());
        }

        public override object DeserializeTuple(MemoryStream stream, TypeAccessor typeAccessor)
        {
            List<object> parameters = new List<object>();
            foreach (var member in typeAccessor.MemberAccessors)
            {
                parameters.Add(Deserialize(null, stream, member.TypeAccessor, member));
            }
            return typeAccessor.Construct(parameters.ToArray());
        }

        public override MemoryStream GetContext(byte[] wireValue)
        {
            return wireValue == null ? new MemoryStream() : new MemoryStream(wireValue);
        }

        public override byte[] GetWireValue(MemoryStream context) => context.ToArray();
    }
}
