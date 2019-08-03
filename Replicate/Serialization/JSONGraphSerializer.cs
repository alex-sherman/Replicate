using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    [Obsolete("Use JSONSerializer instead")]
    public class JSONGraphSerializer : GraphSerializer
    {
        public class JSONIntSerializer : ITypedSerializer
        {
            Regex rx = new Regex(@"[0-9\.\+\-eE]");
            public object Read(Stream stream) => int.Parse(stream.ReadAllString(c => rx.IsMatch("" + c)));
            public void Write(object obj, Stream stream) => stream.WriteString(Convert.ChangeType(obj, typeof(int)).ToString());
        }
        public class JSONBoolSerializer : ITypedSerializer
        {
            public object Read(Stream stream)
            {
                var firstChar = stream.ReadCharOne();
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
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString().ToLower());
        }
        public class JSONFloatSerializer : ITypedSerializer
        {
            Regex rx = new Regex(@"[0-9\.\+\-eE]");
            public object Read(Stream stream)
            {
                string s = stream.ReadAllString(c => rx.IsMatch("" + c));
                return double.Parse(s);
            }
            public void Write(object obj, Stream stream) => stream.WriteString(obj.ToString());
        }

        public class JSONStringSerializer : ITypedSerializer
        {
            public static List<Tuple<string, string>> replacements = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("\\\\", "\\"),
                new Tuple<string, string>("\\\"", "\""),
                new Tuple<string, string>("\\n", "\n"),
                new Tuple<string, string>("\\t", "\t"),
            };
            public static string Escape(string str)
            {
                char[] chars = str.ToCharArray();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < chars.Length; i++)
                {
                    switch (chars[i])
                    {
                        case '\t':
                            sb.Append(@"\t"); break;
                        case '\n':
                            sb.Append(@"\n"); break;
                        case '\r':
                            if (i + 1 < chars.Length && chars[i] == '\n')
                            {
                                sb.Append(@"\n");
                                ++i;
                            }
                            break;
                        case '\\':
                            sb.Append(@"\\"); break;
                        case '"':
                            sb.Append("\\\""); break;
                        default:
                            sb.Append(chars[i]); break;
                    }
                }
                return sb.ToString();
            }
            public static string Unescape(string str)
            {
                char[] chars = str.ToCharArray();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] == '\\' && i + 1 < chars.Length)
                    {
                        switch (chars[i + 1])
                        {
                            case 't':
                                sb.Append("\t"); break;
                            case 'n':
                                sb.Append("\n"); break;
                            case '\\':
                                sb.Append("\\"); break;
                            case '"':
                                sb.Append("\""); break;
                            default:
                                sb.Append(chars[i]); continue;
                        }
                        ++i;
                    }
                    else sb.Append(chars[i]);
                }
                return sb.ToString();
            }
            object ITypedSerializer.Read(Stream stream) => Read(stream);
            public string Read(Stream stream) => Unescape(ParseString(stream));
            public void Write(object obj, Stream stream)
            {
                stream.WriteString("\"");
                stream.WriteString(Escape((string)obj));
                stream.WriteString("\"");
            }

            // TODO: Handle other escape characters
            static string ParseString(Stream stream)
            {
                char last = char.MinValue;
                stream.ReadAllString(IsW);
                CheckAndThrow(stream.ReadCharOne() == '"');
                var result = stream.ReadAllString(c =>
                {
                    var res = c != '"' || last == '\\';
                    last = c;
                    return res;
                });
                CheckAndThrow(stream.ReadCharOne() == '"');
                return result;
            }
        }
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
        static bool IsW(char c) => c == ' ' || c == '\t' || c == '\n' || c == '\r';
        static void CheckAndThrow(bool condition)
        {
            if (!condition)
                throw new SerializationError();
        }
        static bool ReadNull(Stream stream)
        {
            stream.ReadAllString(IsW);
            if (stream.ReadCharOne(true) == 'n')
            {
                CheckAndThrow(stream.ReadChars(4) == "null");
                return true;
            }
            return false;
        }

        public JSONGraphSerializer(ReplicationModel model) : base(model) { }

        public override IRepPrimitive Read(Stream stream, IRepPrimitive value)
        {
            if (ReadNull(stream)) { value.Value = null; return value; }
            //if (value.MarshallMethod == MarshallMethod.Typeless)
            //{
            //    var primType = ReadPrimitiveType(stream);
            //    value.PrimitiveType = primType;
            //    value.Value = serializers[value.PrimitiveType].Read(stream);
            //    return value;
            //}
            //try
            //{
            value.Value = serializers[value.PrimitiveType].Read(stream);
            return value;
            //}
            //catch (Exception e)
            //{
            //    throw new SerializationError(null, e);
            //}
        }

        public override IRepCollection Read(Stream stream, IRepCollection value)
        {
            if (ReadNull(stream)) { value.Value = null; return value; }
            List<object> values = new List<object>();
            if (stream.ReadCharOne() != '[') throw new SerializationError();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == ']') stream.ReadCharOne();
            while (nextChar != ']')
            {
                stream.ReadAllString(IsW);
                values.Add(Read(stream, Model.GetRepNode(null, value.CollectionType, null)).RawValue);
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == ']');
            };
            value.Values = values;
            return value;
        }

        public override IRepObject Read(Stream stream, IRepObject value)
        {
            if (ReadNull(stream)) { value.Value = null; return value; }
            value.EnsureConstructed();
            if (stream.ReadCharOne() != '{') throw new SerializationError();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == '}') stream.ReadCharOne();
            while (nextChar != '}')
            {
                stream.ReadAllString(IsW);
                var name = stringSer.Read(stream);
                if (name == "location")
                {

                }
                stream.ReadAllString(IsW);
                CheckAndThrow(stream.ReadCharOne() == ':');
                stream.ReadAllString(IsW);
                if (value.CanSetMember(name))
                {
                    var childNode = value[name];
                    value[name] = Read(stream, childNode);
                }
                else Read(stream, (IRepNode)RepNodeNoop.Single);
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == '}');
            };
            return value;
        }

        public override void Write(Stream stream, IRepPrimitive value)
        {
            if (value.Value == null)
                stream.WriteString("null");
            else
                serializers[value.PrimitiveType].Write(value.Value, stream);
        }

        public override void Write(Stream stream, IRepCollection value)
        {
            if (value.Value == null)
                stream.WriteString("null");
            else
            {
                stream.WriteString("[");
                bool first = true;
                foreach (var item in value)
                {
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    Write(stream, item);
                }
                stream.WriteString("]");
            }
        }
        public override void Write(Stream stream, IRepObject value)
        {
            if (value.Value == null)
                stream.WriteString("null");
            else
            {
                stream.WriteString("{");
                bool first = true;
                foreach (var member in value)
                {
                    if (member.Value.IsSkipNull() && member.Value.RawValue == null) continue;
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    stream.WriteString("\"");
                    stream.WriteString(member.Key.Name);
                    stream.WriteString("\": ");
                    Write(stream, member.Value);
                }
                stream.WriteString("}");
            }
        }

        public override (MarshallMethod, PrimitiveType?) ReadNodeType(Stream stream)
        {
            stream.ReadAllString(IsW);
            var c = stream.ReadCharOne(true);
            // The 'n' is for null, return it as a null object
            if (c == '{' || c == 'n') return (MarshallMethod.Object, null);
            if (c == '[') return (MarshallMethod.Collection, null);
            return (MarshallMethod.Primitive, ReadPrimitiveType(stream));
        }

        public PrimitiveType ReadPrimitiveType(Stream stream)
        {
            stream.ReadAllString(IsW);
            var c = stream.ReadCharOne(true);
            if (c == '"') return PrimitiveType.String;
            if (c == 't' || c == 'f') return PrimitiveType.Bool;
            return PrimitiveType.Double;
        }
    }
}
