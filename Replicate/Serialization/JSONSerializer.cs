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
    public class JSONSerializer : Serializer, IReplicateSerializer<string>
    {
        class JSONIntSerializer : ITypedSerializer
        {
            Regex rx = new Regex(@"[0-9\.\+\-eE]");
            public object Read(Stream stream) => long.Parse(stream.ReadAllString(c => rx.IsMatch("" + c)));
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }
        class JSONBoolSerializer : ITypedSerializer
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
        class JSONFloatSerializer : ITypedSerializer
        {
            Regex rx = new Regex(@"[1-9\.\+\-eE]");
            public object Read(Stream stream) => double.Parse(stream.ReadAllString(c => rx.IsMatch("" + c)));
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }

        class JSONStringSerializer : ITypedSerializer
        {
            public static List<Tuple<string, string>> replacements = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("\\\\", "\\"),
                new Tuple<string, string>("\\\"", "\""),
                new Tuple<string, string>("\\n", "\n"),
                new Tuple<string, string>("\\t", "\t"),
            };
            static string Escape(string str)
            {
                foreach (var replacement in replacements)
                    str = str.Replace(replacement.Item2, replacement.Item1);
                return str;
            }
            static string Unescape(string str)
            {
                foreach (var replacement in replacements)
                    str = str.Replace(replacement.Item1, replacement.Item2);
                return str;
            }
            public object Read(Stream stream) => Unescape(parseString(stream));
            public void Write(object obj, Stream stream) => stream.WriteString($"\"{Escape((string)obj)}\"");
        }

        public bool ToLowerFieldNames = false;
        public JSONSerializer(ReplicationModel model) : base(model) { }
        static JSONIntSerializer intSer = new JSONIntSerializer();
        Dictionary<Type, ITypedSerializer> serializers = new Dictionary<Type, ITypedSerializer>()
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
            stream.ReadAllString(IsW);
            while(stream.ReadChar(true) != ']')
            {
                stream.ReadAllString(IsW);
                values.Add(Deserialize(null, stream, collectionValueAccessor, null));
                stream.ReadAllString(IsW);
                var nextChar = stream.ReadChar();
                if (nextChar == ']')
                    break;
                CheckAndThrow(nextChar == ',');
            }
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
            if (stream.ReadChar() != '{') throw new SerializationError();
            do
            {
                stream.ReadAllString(IsW);
                if (stream.ReadChar(true) == '}')
                    break;
                var name = parseString(stream);
                stream.ReadAllString(IsW);
                CheckAndThrow(stream.ReadChar() == ':');
                stream.ReadAllString(IsW);
                var memberAccessor = typeAccessor.MemberAccessors.FirstOrDefault(m => MapName(m.Info.Name) == name);
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
                var res = c != '"' || last == '\\';
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
            {
                try
                {
                    return Convert.ChangeType(serializers[type].Read(stream), type);
                }
                catch(Exception e)
                {
                    throw new SerializationError(null, e);
                }
            }
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
