﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Replicate.MetaData;

namespace Replicate.Serialization
{
    public class JSONGraphSerializer : GraphSerializer<Stream, string>
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
        class JSONFloatSerializer : ITypedSerializer
        {
            Regex rx = new Regex(@"[0-9\.\+\-eE]");
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
            public static string Escape(string str)
            {
                foreach (var replacement in replacements)
                    str = str.Replace(replacement.Item2, replacement.Item1);
                return str;
            }
            public static string Unescape(string str)
            {
                foreach (var replacement in replacements)
                    str = str.Replace(replacement.Item1, replacement.Item2);
                return str;
            }
            object ITypedSerializer.Read(Stream stream) => Read(stream);
            public string Read(Stream stream) => Unescape(ParseString(stream));
            public void Write(object obj, Stream stream) => stream.WriteString($"\"{Escape((string)obj)}\"");

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
            {PrimitiveType.Int8, intSer },
            {PrimitiveType.Int32, intSer },
            {PrimitiveType.Float, new JSONFloatSerializer() },
            {PrimitiveType.Double, new JSONFloatSerializer() },
            {PrimitiveType.String, stringSer },
        };
        static Regex ws = new Regex("\\s");
        static bool IsW(char c) => ws.IsMatch("" + c);
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

        public override Stream GetContext(string wireValue)
        {
            var stream = new MemoryStream();
            if(wireValue != null)
            {
                var sw = new StreamWriter(stream);
                sw.Write(wireValue);
                sw.Flush();
                stream.Position = 0;
            }
            return stream;
        }

        public override string GetWireValue(Stream context)
        {
            context.Position = 0;
            return new StreamReader(context).ReadToEnd();
        }

        public override object Read(Stream stream, IRepPrimitive value)
        {
            if (ReadNull(stream)) return null;
            try
            {
                return Convert.ChangeType(serializers[value.PrimitiveType].Read(stream), value.TypeAccessor.Type);
            }
            catch (Exception e)
            {
                throw new SerializationError(null, e);
            }
        }

        public override object Read(Stream stream, IRepCollection value)
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
                values.Add(Read(stream, Model.GetRepNode(null, value.CollectionType)));
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == ']');
            };
            value.Values = values;
            return value.Value;
        }

        public override object Read(Stream stream, IRepObject value)
        {
            if (ReadNull(stream)) return null;
            if (value.Value == null)
                value.Value = value.TypeAccessor.Construct();
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
                var childNode = value[name];
                CheckAndThrow(childNode != null);
                Read(stream, childNode);
                value[name] = childNode;
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == '}');
            };
            return value.Value;
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
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    stream.WriteString($"\"{member.Key}\": ");
                    Write(stream, member.Value);
                }
                stream.WriteString("}");
            }
        }
    }
}
