using Replicate.MetaData;
using System;
using System.Collections;
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
                        case PrimitiveType.SVarInt:
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
        public class ProtoSIntSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream) => ReadSVarInt(stream);
            void ITypedSerializer.Write(object obj, Stream stream)
                => WriteSVarInt((long)Convert.ChangeType(obj, typeof(long)), stream);
        }
        public class ProtoFloatSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream)
            {
                var bytes = new byte[4];
                stream.Read(bytes, 0, 4);
                return BitConverter.ToSingle(bytes, 0);
            }
            void ITypedSerializer.Write(object obj, Stream stream)
                => stream.Write(BitConverter.GetBytes((float)obj), 0, 4);
        }
        public class ProtoDoubleSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream)
            {
                var bytes = new byte[8];
                stream.Read(bytes, 0, 8);
                return BitConverter.ToDouble(bytes, 0);
            }
            void ITypedSerializer.Write(object obj, Stream stream)
                => stream.Write(BitConverter.GetBytes((double)obj), 0, 8);
        }
        public class ProtoStringSerializer : ITypedSerializer
        {
            object ITypedSerializer.Read(Stream stream) =>
                stream.ReadAllString();
            void ITypedSerializer.Write(object obj, Stream stream) =>
                stream.WriteString((string)obj);
        }
        public static void WriteSVarInt(long num, Stream stream)
        {
            ulong zig = ((ulong)num) << 1;
            if (((num >> 63) & 1) == 1) zig ^= ulong.MaxValue;
            WriteVarInt(zig, stream);
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
        public static long ReadSVarInt(Stream stream)
        {
            var num = ReadVarInt(stream);
            return (long)((num >> 1) ^ (((num & 1) == 1) ? ulong.MaxValue : 0));
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

            {PrimitiveType.Bool, intSer },
            {PrimitiveType.Byte, intSer },
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.SVarInt, new ProtoSIntSerializer() },
            {PrimitiveType.Float, new ProtoFloatSerializer() },
            {PrimitiveType.Double, new ProtoDoubleSerializer() },
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
                if (member.TypeAccessor.TypeData.MarshallMethod == MarshallMethod.Collection)
                {
                    var repeated = member.GetValue(obj);
                    if (repeated == null) continue;
                    foreach (var value in (IEnumerable)repeated)
                        WriteObjectEntry(stream, value, key, member);
                }
                else
                    WriteObjectEntry(stream, member.GetValue(obj), key, member);
            }
        }
        private TypeAccessor ObjectMemberType(MemberAccessor member)
        {
            var typeAccessor = member.TypeAccessor;
            if (typeAccessor.TypeData.MarshallMethod == MarshallMethod.Collection)
                return Model.GetCollectionValueAccessor(typeAccessor.Type);
            return typeAccessor;
        }
        private void WriteObjectEntry(Stream stream, object obj, RepKey key, MemberAccessor member)
        {
            var typeAccessor = ObjectMemberType(member);
            var wireType = GetWireType(typeAccessor.TypeData);
            WriteVarInt((ulong)((long)(key.Index.Value << 3) | (long)wireType), stream);
            var subStream = wireType == WireType.Length ? new MemoryStream() : stream;
            Write(subStream, obj, typeAccessor, member);
            if (wireType == WireType.Length)
            {
                subStream.Position = 0;
                WriteVarInt((ulong)subStream.Length, stream);
                // TODO: This could be SG
                subStream.CopyTo(stream);
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
                var member = typeAccessor.Members[key];
                WireType type = (WireType)(tag & 7);
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
                var memberTypeAccessor = ObjectMemberType(member);
                var value = Read(null, subStream, memberTypeAccessor, member);
                if (member.TypeAccessor.TypeData.MarshallMethod == MarshallMethod.Collection)
                    AddToCollection(obj, value, member);
                else
                    member.SetValue(obj, value);
            }
            return obj;
        }

        private void AddToCollection(object parent, object value, MemberAccessor member)
        {
            var type = member.Type;
            var collectionType = type.GetInterface("ICollection`1");
            var addMeth = collectionType?.GetMethod("Add");
            if (addMeth == null) throw new ReplicateError($"Cannot deserialize repeated fields into {type.Name}");
            var collection = member.GetValue(parent);
            if (collection == null)
            {
                collection = member.TypeAccessor.Construct();
                member.SetValue(parent, collection);
            }
            addMeth.Invoke(collection, new[] { value });
        }

        // Protobuf doesn't support collections that aren't a member of a message
        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
        // Protobuf doesn't support collections that aren't a member of a message
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
