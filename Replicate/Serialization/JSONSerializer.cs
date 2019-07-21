﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Replicate.MetaData;
using static Replicate.Serialization.JSONGraphSerializer;

namespace Replicate.Serialization
{
    public class JSONSerializer : Serializer<Stream, string>
    {
        public bool ToLowerFieldNames = false;
        public JSONSerializer(ReplicationModel model) : base(model) { }
        static JSONIntSerializer intSer = new JSONIntSerializer();
        static JSONStringSerializer stringSer = new JSONStringSerializer();
        readonly Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {

            {PrimitiveType.Bool, new JSONBoolSerializer() },
            {PrimitiveType.Byte, intSer },
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.Float, new JSONFloatSerializer() },
            {PrimitiveType.Double, new JSONFloatSerializer() },
            {PrimitiveType.String, stringSer },
        };
        static void CheckAndThrow(bool condition)
        {
            if (!condition)
                throw new SerializationError();
        }

        static Regex ws = new Regex("\\s");
        static bool IsW(char c) => ws.IsMatch("" + c);

        public override object DeserializeCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor)
        {
            if (ReadNull(stream)) return null;
            List<object> values = new List<object>();
            if (stream.ReadCharOne() != '[') throw new SerializationError();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == ']') stream.ReadCharOne();
            while (nextChar != ']')
            {
                stream.ReadAllString(IsW);
                values.Add(Deserialize(null, stream, collectionValueAccessor, null));
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == ']');
            };
            return CollectionUtil.FillCollection(obj, typeAccessor.Type, values);
        }
        string MapName(string fieldName)
        {
            if (ToLowerFieldNames)
                return fieldName.ToLower();
            return fieldName;
        }
        public override object DeserializeObject(object obj, Stream stream, TypeAccessor typeAccessor)
        {
            if (ReadNull(stream)) return null;
            if (obj == null)
                obj = typeAccessor.Construct();
            if (stream.ReadCharOne() != '{') throw new SerializationError();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == '}') stream.ReadCharOne();
            while (nextChar != '}')
            {
                stream.ReadAllString(IsW);
                var name = stringSer.Read(stream);
                stream.ReadAllString(IsW);
                CheckAndThrow(stream.ReadCharOne() == ':');
                stream.ReadAllString(IsW);
                var memberAccessor = typeAccessor.MemberAccessors.FirstOrDefault(m => MapName(m.Info.Name) == name);
                CheckAndThrow(memberAccessor != null);
                var value = Deserialize(memberAccessor.GetValue(obj), stream, memberAccessor.TypeAccessor, memberAccessor);
                memberAccessor.SetValue(obj, value);
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == '}');
            };
            return obj;
        }

        public override object DeserializePrimitive(Stream stream, TypeAccessor typeAccessor)
        {
            if (ReadNull(stream)) return null;
            try
            {
                return typeAccessor.Coerce(serializers[typeAccessor.TypeData.PrimitiveType].Read(stream));
            }
            catch (Exception e)
            {
                throw new SerializationError(null, e);
            }
        }

        bool ReadNull(Stream stream)
        {
            stream.ReadAllString(IsW);
            if (stream.ReadCharOne(true) == 'n')
            {
                CheckAndThrow(stream.ReadChars(4) == "null");
                return true;
            }
            return false;
        }

        public override object DeserializeTuple(Stream stream, TypeAccessor typeAccessor)
        {
            throw new NotImplementedException();
        }

        public override void SerializeCollection(Stream stream, object obj, TypeAccessor collectionValueType)
        {
            if (obj == null)
                SerializePrimitive(stream, null, null);
            else
            {
                stream.WriteString("[");
                bool first = true;
                foreach (var item in (IEnumerable)obj)
                {
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    Serialize(stream, item, collectionValueType, null);
                }
                stream.WriteString("]");
            }
        }

        public override void SerializeObject(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            if (obj == null)
                SerializePrimitive(stream, null, null);
            else
            {
                stream.WriteString("{");
                bool first = true;
                foreach (var member in typeAccessor.MemberAccessors)
                {
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    stream.WriteString($"\"{MapName(member.Info.Name)}\": ");
                    Serialize(stream, member.GetValue(obj), member.TypeAccessor, member);
                }
                stream.WriteString("}");
            }
        }

        public override void SerializePrimitive(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            if (obj == null)
                stream.WriteString("null");
            else
                serializers[typeAccessor.TypeData.PrimitiveType].Write(obj, stream);
        }

        public override void SerializeTuple(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            throw new NotImplementedException();
        }

        public override Stream GetContext(string wireValue)
        {
            var stream = new MemoryStream();
            if (wireValue != null)
            {
                var sw = new StreamWriter(stream);
                sw.Write(wireValue);
                sw.Flush();
                stream.Position = 0;
            }
            return stream;
        }

        public override string GetWireValue(Stream stream)
        {
            stream.Position = 0;
            return new StreamReader(stream).ReadToEnd();
        }
    }
}
