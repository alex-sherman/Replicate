using System;
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
    public class JSONSerializer : Serializer
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

        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor)
        {
            if (ReadNull(stream)) return null;
            if (typeAccessor.IsDictObj)
            {
                if (obj == null) obj = typeAccessor.Construct();
                var dict = obj as IDictionary;
                ReadObject(stream, name =>
                {
                    var value = Read(dict[name], stream, Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[1]), null);
                    dict[name] = value;
                });
                return obj;
            }

            List<object> values = new List<object>();
            if (stream.ReadCharOne() != '[') throw new SerializationError();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == ']') stream.ReadCharOne();
            while (nextChar != ']')
            {
                stream.ReadAllString(IsW);
                values.Add(Read(null, stream, collectionValueAccessor, null));
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

        public void ReadObject(Stream stream, Action<string> onEntry)
        {
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
                onEntry(name);
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(nextChar == ',' || nextChar == '}');
            };
        }
        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor)
        {
            if (ReadNull(stream)) return null;
            if (obj == null) obj = typeAccessor.Construct();
            ReadObject(stream, name =>
            {
                var memberAccessor = typeAccessor.MemberAccessors.FirstOrDefault(m => MapName(m.Info.Name) == name);
                CheckAndThrow(memberAccessor != null);
                var value = Read(memberAccessor.GetValue(obj), stream, memberAccessor.TypeAccessor, memberAccessor);
                memberAccessor.SetValue(obj, value);
            });
            return obj;
        }

        public override object ReadPrimitive(Stream stream, TypeAccessor typeAccessor)
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
        IEnumerable<(MemberKey key, object value, TypeAccessor type, MemberAccessor member)> getDictValues(IDictionary dict, TypeAccessor typeAccessor)
        {
            foreach (var key in dict.Keys)
            {
                yield return (key as string, dict[key], typeAccessor, null);
            }
        }
        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType)
        {
            if (obj == null)
                WritePrimitive(stream, null, null);
            else
            {
                if(typeAccessor.IsDictObj)
                {
                    SerializeObject(stream, getDictValues(obj as IDictionary, Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[1])));
                    return;
                }
                stream.WriteString("[");
                bool first = true;
                foreach (var item in (IEnumerable)obj)
                {
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    Write(stream, item, collectionValueType, null);
                }
                stream.WriteString("]");
            }
        }

        public void SerializeObject(Stream stream, IEnumerable<(MemberKey key, object value, TypeAccessor type, MemberAccessor member)> obj)
        {
            if (obj == null)
                WritePrimitive(stream, null, null);
            else
            {
                stream.WriteString("{");
                bool first = true;
                foreach (var (key, value, type, member) in obj)
                {
                    if ((member?.SkipNull ?? false) && value == null) continue;
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    stream.WriteString($"\"{MapName(key.Name)}\": ");
                    Write(stream, value, type, member);
                }
                stream.WriteString("}");
            }
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            var objectSet = obj == null ? null : typeAccessor.TypeData.Keys.Select(key =>
            {
                var member = typeAccessor[key];
                return (key, member.GetValue(obj), member.TypeAccessor, member);
            });
            SerializeObject(stream, objectSet);
        }

        public override void WritePrimitive(Stream stream, object obj, TypeAccessor typeAccessor)
        {
            if (obj == null)
                stream.WriteString("null");
            else
                serializers[typeAccessor.TypeData.PrimitiveType].Write(obj, stream);
        }

        public override void WriteBlob(Stream stream, Blob obj, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }

        public override Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            throw new NotImplementedException();
        }
    }
}
