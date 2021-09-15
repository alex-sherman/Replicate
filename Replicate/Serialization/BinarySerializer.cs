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
    public class BinaryNullTermStringSerializer : ITypedSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadAllString(c => c != 0);
        }
        public void Write(object obj, Stream stream)
        {
            byte[] bytes = Encoding.ASCII.GetBytes((string)obj);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
        }
    }
    public class BinarySerializer : Serializer
    {
        public readonly bool TypePrefix = true;
        public BinarySerializer(ReplicationModel model = null, bool typePrefix = true) : base(model) { TypePrefix = typePrefix; }
        static BinaryIntSerializer intSer = new BinaryIntSerializer();
        public Dictionary<PrimitiveType, ITypedSerializer> Serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.Byte, new BinaryByteSerializer() },
            {PrimitiveType.Bool, intSer },
            {PrimitiveType.String, new BinaryStringSerializer() },
            {PrimitiveType.Double, new BinaryFloatSerializer() },
            {PrimitiveType.Float, new BinaryFloatSerializer() },
        };

        public override void WritePrimitive(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable))
                stream.WriteInt32((obj == null) ? 0 : 1);
            if (obj == null) return;
            if (Serializers.TryGetValue(typeAccessor.TypeData.PrimitiveType, out var ser))
                ser.Write(obj, stream);
            else
                throw new SerializationError();
        }

        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor)
        {
            var count = 0;
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable))
                stream.WriteInt32((obj == null) ? 0 : 1);
            if (obj == null) return;
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
                Serializers[PrimitiveType.String].Write(key.Name, stream);
            }
            else stream.WriteByte(255);
        }
        RepKey ReadKey(Stream stream)
        {
            var b = (byte)stream.ReadByte();
            if (b == 255) return default(RepKey);
            if (b != 254) return b;
            return (string)Serializers[PrimitiveType.String].Read(stream);
        }

        private void SerializeObject(Stream stream, IEnumerable<(RepKey key, object value, MemberAccessor member)> obj)
        {
            int count = 0;
            var countPos = stream.Position;
            if (TypePrefix) stream.Position += 4;
            foreach (var tuple in obj)
            {
                if (TypePrefix) WriteKey(stream, tuple.key);
                Write(stream, tuple.value, tuple.member.TypeAccessor, tuple.member);
                count++;
            }
            if (TypePrefix)
            {
                var endPos = stream.Position; stream.Position = countPos;
                stream.WriteInt32(count); stream.Position = endPos;
            }
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable))
                stream.WriteInt32((obj == null) ? 0 : 1);
            if (obj == null) return;
            var objectSet = obj == null ? null : typeAccessor.TypeData.Keys.Select(key =>
            {
                var member = typeAccessor[key];
                return (key, member.GetValue(obj), member);
            });
            SerializeObject(stream, objectSet);
        }

        public override object ReadPrimitive(Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable) && (stream.ReadInt32() == 0)) return null;
            return Model.Coerce(typeAccessor, (Serializers[typeAccessor.TypeData.PrimitiveType].Read(stream)));
        }

        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable) && (stream.ReadInt32() == 0)) return null;
            int count = TypePrefix ? stream.ReadInt32() : typeAccessor.Members.Count;
            if (count == -1) return null;
            if (obj == null)
                obj = typeAccessor.Construct();
            for (int i = 0; i < count; i++)
            {
                int id = TypePrefix ? stream.ReadByte() : i;
                var member = typeAccessor.Members[id];
                member.SetValue(obj, Read(member.GetValue(obj), stream, member.TypeAccessor, member));
            }
            return obj;
        }

        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor, MemberAccessor memberAccessor)
        {
            if ((memberAccessor?.IsNullable ?? typeAccessor.IsNullable) && (stream.ReadInt32() == 0)) return null;
            int count = stream.ReadInt32();
            if (count == -1) return null;
            return CollectionUtil.FillCollection(obj, typeAccessor.Type, Enumerable.Range(0, count)
                .Select(i => Read(null, stream, collectionValueAccessor, null))
                .ToList());
        }

        public override void WriteBlob(Stream stream, Blob blob, MemberAccessor member)
        {
            var blobStream = blob?.Stream;
            stream.WriteByte(blobStream == null ? (byte)255 : (byte)0);
            if (blobStream == null) return;
            stream.WriteInt32((int)blobStream.Length);
            stream.CopyTo(stream);
        }

        public override Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (stream.ReadByte() == 255) return null;
            // TODO: Could use SubStream
            var count = stream.ReadInt32();
            var bytes = new byte[count];
            stream.Read(bytes, 0, count);
            obj = obj ?? (Blob)typeAccessor.Construct();
            obj.SetStream(new MemoryStream(bytes));
            return obj;
        }
    }
}
