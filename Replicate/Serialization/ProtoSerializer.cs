using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class ProtoSerializer : Serializer
    {
        const int NVARINT = 10;
        public class ProtoIntSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream) => ReadVarInt(stream);
            void ITypedSerializer.Write(object obj, Stream stream)
                => WriteVarInt((ulong)Convert.ChangeType(obj, typeof(ulong)), stream);
        }
        public class ProtoStringSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream)
            {
                int length = (int)ReadVarInt(stream);
                return stream.ReadChars(length);
            }
            void ITypedSerializer.Write(object obj, Stream stream)
            {
                var s = (string)obj;
                WriteVarInt((ulong)s.Length, stream);
                stream.WriteString(s);
            }
        }
        public static void WriteVarInt(ulong num, Stream stream)
        {
            int n = 1;
            var bytes = new byte[NVARINT];
            for (int i = 0; i < NVARINT; i++)
            {
                bytes[i] = (byte)((num >> 7 * i) & 0x7F);
                if (bytes[i] != 0) n = i + 1;
            }
            for (int i = 0; i < n; i++)
                stream.WriteByte((byte)((byte)(i == (n - 1) ? 0 : 0x80) | bytes[i]));
        }
        public static ulong ReadVarInt(Stream stream)
        {
            ulong num = 0;
            byte b;
            for (int i = 0; i < NVARINT; i++)
            {
                b = (byte)stream.ReadByte();
                num += (ulong)(b & 0x7f) << (i * 7);
                if ((b & 0x80) == 0) break;
            }
            return num;
        }
        static ProtoIntSerializer intSer = new ProtoIntSerializer();
        readonly Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {

            //{PrimitiveType.Bool, new JSONBoolSerializer() },
            {PrimitiveType.Byte, intSer },
            {PrimitiveType.VarInt, intSer },
            //{PrimitiveType.Float, new JSONFloatSerializer() },
            //{PrimitiveType.Double, new JSONFloatSerializer() },
            {PrimitiveType.String, new ProtoStringSerializer() },
        };
        public ProtoSerializer(ReplicationModel model) : base(model) { }

        public override void WritePrimitive(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (obj == null) return;
            serializers[typeAccessor.TypeData.PrimitiveType].Write(obj, stream);
        }
        public override object ReadPrimitive(Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            return serializers[typeAccessor.TypeData.PrimitiveType].Read(stream);
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {

        }
        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionAccessor, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }

        public override void WriteBlob(Stream stream, Blob blob, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
        public override Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
    }
}
