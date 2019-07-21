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
    public class BinaryGraphSerializer : GraphSerializer<MemoryStream, MemoryStream>
    {
        public BinaryGraphSerializer(ReplicationModel model) : base(model) { }
        static BinaryIntSerializer intSer = new BinaryIntSerializer();
        static Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {
            {PrimitiveType.Int32, intSer },
            {PrimitiveType.Int8, intSer },
            {PrimitiveType.Bool, intSer },
            {PrimitiveType.String, new BinaryStringSerializer() },
            {PrimitiveType.Double, new BinaryFloatSerializer() },
            {PrimitiveType.Float, new BinaryFloatSerializer() },
        };

        //public override void SerializeTuple(Stream stream, object obj, TypeAccessor typeAccessor)
        //{
        //    foreach (var member in typeAccessor.MemberAccessors)
        //        Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
        //}


        //public override object DeserializeTuple(Stream stream, Type type, TypeAccessor typeAccessor)
        //{
        //    List<object> parameters = new List<object>();
        //    List<Type> paramTypes = new List<Type>();
        //    foreach (var member in typeAccessor.MemberAccessors)
        //    {
        //        paramTypes.Add(member.Type);
        //        parameters.Add(Deserialize(null, stream, member.TypeAccessor, member));
        //    }
        //    return type.GetConstructor(paramTypes.ToArray()).Invoke(parameters.ToArray());
        //}

        public override MemoryStream GetContext(MemoryStream wireValue)
        {
            return wireValue ?? new MemoryStream();
        }

        public override MemoryStream GetWireValue(MemoryStream context)
        {
            context.Position = 0;
            return context ?? new MemoryStream();
        }

        public override void Write(MemoryStream stream, IRepPrimitive value)
        {
            if (value.RawValue == null)
                stream.WriteByte(0);
            else
            {
                stream.WriteByte(1);
                serializers[value.PrimitiveType].Write(value.RawValue, stream);
            }
        }

        public override void Write(MemoryStream stream, IRepCollection value)
        {
            if (value.Value == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            var lengthPos = stream.Position;
            stream.Position += 4;
            var count = 0;
            foreach (var item in value)
            {
                Write(stream, item);
                count++;
            }
            var reset = stream.Position;
            stream.Position = lengthPos;
            stream.WriteInt32(count);
            stream.Position = reset;
        }
        void Write(Stream stream, MemberKey key)
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
        public override void Write(MemoryStream stream, IRepObject value)
        {
            if (value.Value == null)
            {
                stream.WriteInt32(-1);
                return;
            }
            var lengthPosition = stream.Position;
            stream.Position += 4;
            var count = 0;
            foreach (var member in value)
            {
                Write(stream, member.Key);
                Write(stream, member.Value);
                count++;
            }
            var endPosition = stream.Position;
            stream.Position = lengthPosition;
            stream.WriteInt32(count);
            stream.Position = endPosition;
        }

        public override (MarshallMethod, PrimitiveType?) ReadNodeType(MemoryStream context)
        {
            throw new NotImplementedException();
        }

        public override IRepPrimitive Read(MemoryStream stream, IRepPrimitive value)
        {
            if (stream.ReadByte() == 0) { value.Value = null; return value; }
            value.Value = serializers[value.PrimitiveType].Read(stream);
            return value;
        }

        public override IRepCollection Read(MemoryStream stream, IRepCollection value)
        {
            int count = stream.ReadInt32();
            if (count == -1) { value.Value = null; return value; }
            value.Values = Enumerable.Range(0, count)
                .Select(i => Read(stream, Model.GetRepNode(null, value.CollectionType, null)).RawValue);
            return value;
        }

        public override IRepObject Read(MemoryStream stream, IRepObject value)
        {
            int count = stream.ReadInt32();
            if (count == -1) { value.Value = null; return value; }
            value.EnsureConstructed();
            for (int i = 0; i < count; i++)
            {
                var id = ReadKey(stream);
                var inner = value[id];
                if (value.CanSetMember(id))
                {
                    var childNode = value[id];
                    value[id] = Read(stream, childNode);
                }
                else Read(stream, (IRepNode)RepNodeNoop.Single);
            }
            return value;
        }
    }
}
