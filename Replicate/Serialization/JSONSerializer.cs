using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    [Obsolete("Replaced by JSONGraphSerializer")]
    public class JSONSerializer : Serializer, IReplicateSerializer<string>
    {
        public bool ToLowerFieldNames = false;
        public JSONSerializer(ReplicationModel model) : base(model) { }
        static JSONGraphSerializer.JSONIntSerializer intSer = new JSONGraphSerializer.JSONIntSerializer();
        static JSONGraphSerializer.JSONStringSerializer stringSer = new JSONGraphSerializer.JSONStringSerializer();
        Dictionary<Type, ITypedSerializer> serializers = new Dictionary<Type, ITypedSerializer>()
        {
            {typeof(bool), new JSONGraphSerializer.JSONBoolSerializer() },
            {typeof(byte), intSer },
            {typeof(short), intSer },
            {typeof(ushort), intSer },
            {typeof(int), intSer },
            {typeof(uint), intSer },
            {typeof(long), intSer },
            {typeof(ulong), intSer },
            {typeof(string), stringSer },
            {typeof(float), new JSONGraphSerializer.JSONFloatSerializer() },
        };
        static void CheckAndThrow(bool condition)
        {
            if (!condition)
                throw new SerializationError();
        }

        static Regex ws = new Regex("\\s");
        static bool IsW(char c) => ws.IsMatch("" + c);

        public override object DeserializeCollection(object obj, Stream stream, Type type, TypeAccessor collectionValueAccessor)
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
            return FillCollection(obj, type, values);
        }
        string MapName(string fieldName)
        {
            if (ToLowerFieldNames)
                return fieldName.ToLower();
            return fieldName;
        }
        public override object DeserializeObject(object obj, Stream stream, Type type, TypeAccessor typeAccessor)
        {
            if (ReadNull(stream)) return null;
            if (obj == null)
                obj = Activator.CreateInstance(type);
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

        public override object DeserializePrimitive(Stream stream, Type type)
        {
            if (ReadNull(stream)) return null;
            if (serializers.ContainsKey(type))
            {
                try
                {
                    return Convert.ChangeType(serializers[type].Read(stream), type);
                }
                catch (Exception e)
                {
                    throw new SerializationError(null, e);
                }
            }
            return null;
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

        public override object DeserializeTuple(Stream stream, Type type, TypeAccessor typeAccessor)
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

        public override void SerializePrimitive(Stream stream, object obj, Type type)
        {
            if (obj == null)
                stream.WriteString("null");
            else
                serializers[type].Write(obj, stream);
        }

        public override void SerializeTuple(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            throw new NotImplementedException();
        }

        public string Serialize(Type type, object obj)
        {
            var stream = new MemoryStream();
            Serialize(stream, type, obj);
            stream.Position = 0;
            return new StreamReader(stream).ReadToEnd();
        }

        public object Deserialize(Type type, string message)
        {
            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            sw.Write(message);
            sw.Flush();
            stream.Position = 0;
            return Deserialize(stream, type);
        }
    }
}
