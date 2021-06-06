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
        public object Read(Stream stream) => stream.ReadInt32();
        public void Write(object obj, Stream stream) => stream.WriteInt32(Convert.ToInt32(obj));
    }
    public class BinaryByteSerializer : ITypedSerializer
    {
        public object Read(Stream stream) => stream.ReadByte();
        public void Write(object obj, Stream stream) => stream.WriteByte(Convert.ToByte(obj));
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
    public class BinarySerializer : Serializer
    {
        public BinarySerializer(ReplicationModel model = null) : base(model) { }
        static BinaryIntSerializer intSer = new BinaryIntSerializer();
        static Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.Byte, new BinaryByteSerializer() },
            {PrimitiveType.Bool, intSer },
            {PrimitiveType.String, new BinaryStringSerializer() },
            {PrimitiveType.Double, new BinaryFloatSerializer() },
            {PrimitiveType.Float, new BinaryFloatSerializer() },
        };

        public override void WritePrimitive(Stream stream, object obj, TypeAccessor type)
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

        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType)
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
                Write(stream, item, collectionValueType, null);
        }

        void WriteKey(Stream stream, RepKey key)
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
        RepKey ReadKey(Stream stream)
        {
            var b = (byte)stream.ReadByte();
            if (b == 255) return default(RepKey);
            if (b != 254) return b;
            return (string)serializers[PrimitiveType.String].Read(stream);
        }

        public void SerializeObject(Stream stream, IEnumerable<(RepKey key, object value, MemberAccessor member)> obj)
        {
            if (obj == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            int count = 0;
            var countPos = stream.Position; stream.Position += 4;
            foreach (var tuple in obj)
            {
                WriteKey(stream, tuple.key);
                Write(stream, tuple.value, tuple.member.TypeAccessor, tuple.member);
                count++;
            }
            var endPos = stream.Position; stream.Position = countPos;
            stream.WriteInt32(count); stream.Position = endPos;
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            var objectSet = obj == null ? null : typeAccessor.TypeData.Keys.Select(key =>
            {
                var member = typeAccessor[key];
                return (key, member.GetValue(obj), member);
            });
            SerializeObject(stream, objectSet);
        }

        public override object ReadPrimitive(Stream stream, TypeAccessor typeAccessor)
        {
            var isNull = stream.ReadByte();
            if (isNull == 0) return null;
            return Model.Coerce(typeAccessor, (serializers[typeAccessor.TypeData.PrimitiveType].Read(stream)));
        }

        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            if (obj == null)
                obj = typeAccessor.Construct();
            for (int i = 0; i < count; i++)
            {
                int id = stream.ReadByte();
                var member = typeAccessor.Members[id];
                member.SetValue(obj, Read(member.GetValue(obj), stream, member.TypeAccessor, member));
            }
            return obj;
        }

        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor)
        {
            int count = stream.ReadInt32();
            if (count == -1) return null;
            return CollectionUtil.FillCollection(obj, typeAccessor.Type, Enumerable.Range(0, count)
                .Select(i => Read(null, stream, collectionValueAccessor, null))
                .ToList());
        }

        public override void WriteBlob(Stream stream, Blob blob, MemberAccessor member)
        {
            stream.WriteByte(blob?.Type == null ? (byte)255 : (byte)0);
            if (blob?.Type != null)
            {
                var typeAccessor = Model.GetTypeAccessor(blob.Type);
                Write(stream, Model.GetId(blob.Type), Model.GetTypeAccessor(typeof(TypeId)), null);
                var bytes = this.Serialize(blob.Type, blob.Value);
                stream.WriteInt32((int)bytes.Length);
                bytes.CopyTo(stream);
            }
        }

        public override Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (stream.ReadByte() == 255) return null;
            var typeId = (TypeId)Read(null, stream, Model.GetTypeAccessor(typeof(TypeId)), null);
            var type = Model.GetType(typeId);
            var count = stream.ReadInt32();
            var bytes = new byte[count];
            stream.Read(bytes, 0, count);
            obj = obj ?? (Blob)typeAccessor.Construct();
            obj.SetWireValue(type, new MemoryStream(bytes), this);
            return obj;
        }
    }
}
