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
    public class JSONSerializer : Serializer
    {
        class JSONIntSerializer : IReplicateSerializer
        {
            Regex rx = new Regex(@"[1-9\.\+\-eE]");
            public object Read(Stream stream) => long.Parse(stream.ReadAllString(c => rx.IsMatch("" + c)));
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }
        class JSONBoolSerializer : IReplicateSerializer
        {
            public object Read(Stream stream)
            {
                var firstChar = stream.ReadChar();
                if (firstChar == 'f')
                {
                    CheckAndThrow(stream.ReadChars(4) == "alse");
                    return false;
                }
                if (firstChar == 't')
                {
                    CheckAndThrow(stream.ReadChars(3) == "rue");
                    return true;
                }
                throw new SerializationError();
            }
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }
        class JSONFloatSerializer : IReplicateSerializer
        {
            Regex rx = new Regex(@"[1-9\.\+\-eE]");
            public object Read(Stream stream) => double.Parse(stream.ReadAllString(c => rx.IsMatch("" + c)));
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }
        class JSONStringSerializer : IReplicateSerializer
        {
            public object Read(Stream stream) => parseString(stream);
            public void Write(object obj, Stream stream) => stream.WriteString($"\"{(string)obj}\"");
        }
        public JSONSerializer(ReplicationModel model) : base(model) { }
        static JSONIntSerializer intSer = new JSONIntSerializer();
        Dictionary<Type, IReplicateSerializer> serializers = new Dictionary<Type, IReplicateSerializer>()
        {
            {typeof(bool), new JSONBoolSerializer() },
            {typeof(byte), intSer },
            {typeof(short), intSer },
            {typeof(ushort), intSer },
            {typeof(int), intSer },
            {typeof(uint), intSer },
            {typeof(long), intSer },
            {typeof(ulong), intSer },
            {typeof(string), new JSONStringSerializer() },
            {typeof(float), new JSONFloatSerializer() },
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
            if (stream.ReadChar() != '[') throw new SerializationError();
            do
            {
                stream.ReadAllString(IsW);
                values.Add(Deserialize(null, stream, collectionValueAccessor, null));
                stream.ReadAllString(IsW);
            } while (stream.ReadChar() == ',');
            return FillCollection(obj, type, values);
        }

        public override object DeserializeObject(object obj, Stream stream, Type type, TypeAccessor typeAccessor)
        {
            if (ReadNull(stream)) return null;
            if (obj == null)
                obj = Activator.CreateInstance(type);
            if (stream.ReadChar() != '{') throw new SerializationError();
            do
            {
                var name = parseString(stream);
                stream.ReadAllString(IsW);
                CheckAndThrow(stream.ReadChar() == ':');
                stream.ReadAllString(IsW);
                var memberAccessor = typeAccessor.MemberAccessors.FirstOrDefault(m => m.Info.Name == name);
                CheckAndThrow(memberAccessor != null);
                var value = Deserialize(memberAccessor.GetValue(obj), stream, memberAccessor.TypeAccessor, memberAccessor);
                memberAccessor.SetValue(obj, value);
                stream.ReadAllString(IsW);
            } while (stream.ReadChar() == ',');
            return obj;
        }

        // TODO: Handle other escape characters
        static string parseString(Stream stream)
        {
            char last = char.MinValue;
            stream.ReadAllString(IsW);
            CheckAndThrow(stream.ReadChar() == '"');
            var result = stream.ReadAllString(c =>
            {
                var res = c != '"' || last == char.MinValue || last == '\\';
                last = c;
                return res;
            });
            CheckAndThrow(stream.ReadChar() == '"');
            return result;
        }

        public override object DeserializePrimitive(Stream stream, Type type)
        {
            if (ReadNull(stream)) return null;
            if (serializers.ContainsKey(type))
                return Convert.ChangeType(serializers[type].Read(stream), type);
            return null;
        }

        bool ReadNull(Stream stream)
        {
            stream.ReadAllString(IsW);
            if (stream.ReadChar(true) == 'n')
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
                    stream.WriteString($"\"{member.Info.Name}\": ");
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
    }
}
