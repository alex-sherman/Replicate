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
        public enum WireType
        {
            VarInt = 0,
            Bit64 = 1,
            Length = 2,
            Bit32 = 5
        }
        public static WireType GetWireType(TypeData type)
        {
            switch (type.MarshallMethod)
            {
                case MarshallMethod.Primitive:
                    switch (type.PrimitiveType)
                    {
                        case PrimitiveType.Bool:
                        case PrimitiveType.Byte:
                        case PrimitiveType.VarInt:
                            return WireType.VarInt;
                        case PrimitiveType.Float:
                            return WireType.Bit32;
                        case PrimitiveType.Double:
                            return WireType.Bit64;
                        case PrimitiveType.String:
                            return WireType.Length;
                        default:
                            break;
                    }
                    break;
                case MarshallMethod.Object:
                case MarshallMethod.Blob:
                    return WireType.Length;
                // Collections are a special case and don't have a wire type
                case MarshallMethod.Collection:
                default:
                    break;
            }
            throw new NotImplementedException();
        }
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
            return Model.Coerce(typeAccessor, serializers[typeAccessor.TypeData.PrimitiveType].Read(stream));
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (obj == null) return;
            foreach ((RepKey key, MemberAccessor member) in typeAccessor.Members)
            {
                // TODO
                if (member.TypeAccessor.TypeData.MarshallMethod == MarshallMethod.Collection) continue;
                var wireType = GetWireType(member.TypeAccessor.TypeData);
                WriteVarInt((ulong)((long)(key.Index.Value << 3) | (long)wireType), stream);
                Write(stream, member.GetValue(obj), member.TypeAccessor, member);
            }
        }
        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (stream.Position == stream.Length) return null;
            if (obj == null) obj = typeAccessor.Construct();
            while (stream.Position < stream.Length)
            {
                ulong tag = ReadVarInt(stream);
                int key = (int)(tag >> 3);
                WireType type = (WireType)(tag & 7);
                var member = typeAccessor.Members[key];
                long length = type switch
                {
                    WireType.Bit64 => 8,
                    WireType.Length => (long)ReadVarInt(stream),
                    WireType.Bit32 => 4,
                    _ => 0,
                };
                if (member == null)
                {
                    if (type == WireType.VarInt) ReadVarInt(stream);
                    stream.Position += length;
                    continue;
                }
                var subStream = length != 0 ? new SubStream(stream, length) : stream;
                var value = Read(null, subStream, member.TypeAccessor, member);
                member.SetValue(obj, value);
            }
            return obj;
        }

        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor)
        {
            // Protobuf doesn't support collections that aren't a member of a message
            if (memberAccessor == null) throw new NotImplementedException();
        }
        // Read collection in protobuf is more like ReadCollectionElement
        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionAccessor, MemberAccessor memberAccessor)
        {
            if (obj == null) obj = typeAccessor.Construct();
            var value = Read(null, stream, collectionAccessor, memberAccessor);
            typeAccessor.Type.GetMethod("Add").Invoke(obj, new[] { value });
            return obj;
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
