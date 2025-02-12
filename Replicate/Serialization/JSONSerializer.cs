using Replicate.MetaData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static Replicate.Serialization.JSONGraphSerializer;

namespace Replicate.Serialization {
    public class JSONSerializer : Serializer {
        public struct Configuration {
            public bool Strict;
            public (Func<string, string> To, Func<string, string> From) KeyConvert;
        }
        public static readonly Configuration DefaultConfig = new Configuration() { Strict = true };
        public readonly Configuration Config;
        public JSONSerializer(ReplicationModel model, Configuration? config = null) : base(model) { Config = config ?? DefaultConfig; }
        static JSONIntSerializer intSer = new JSONIntSerializer();
        static JSONStringSerializer stringSer = new JSONStringSerializer();
        readonly Dictionary<PrimitiveType, ITypedSerializer> serializers = new Dictionary<PrimitiveType, ITypedSerializer>()
        {

            {PrimitiveType.Bool, new JSONBoolSerializer() },
            {PrimitiveType.Byte, intSer },
            {PrimitiveType.VarInt, intSer },
            {PrimitiveType.SVarInt, intSer },
            {PrimitiveType.Float, new JSONFloatSerializer() },
            {PrimitiveType.Double, new JSONFloatSerializer() },
            {PrimitiveType.String, stringSer },
        };

        private ThreadLocal<Stack<object>> ObjectStack = new ThreadLocal<Stack<object>>();
        private struct ObjectTracker : IDisposable {
            object Obj;
            JSONSerializer Ser;
            public ObjectTracker(JSONSerializer ser, object obj) {
                Obj = obj;
                Ser = ser;
                Ser.ObjectStack.Value.Push(Obj);
            }
            public void Dispose() {
                var obj = Ser.ObjectStack.Value.Pop();
                Debug.Assert(obj == Obj);
                if (Ser.ObjectStack.Value.Count == 0) Ser.ObjectStack.Value = null;
            }
        }

        static void CheckAndThrow(Stream stream, bool condition, string message = null) {
            if (!condition) {
                if (message == null)
                    throw new SerializationError(stream);
                else
                    throw new SerializationError(message, stream);
            }
        }

        static Regex ws = new Regex("\\s");
        static bool IsW(char c) => ws.IsMatch("" + c);

        private bool ReadCollection(Stream stream, Action onEntry) {
            if (stream.ReadCharOne(true) != '[') {
                if (Config.Strict) throw new SerializationError();
                ReadToken(stream);
                return false;
            }
            stream.ReadCharOne();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == ']') stream.ReadCharOne();
            while (nextChar != ']') {
                stream.ReadAllString(IsW);
                // Handle trailing commas.
                var peekChar = stream.ReadCharOne(true);
                if (peekChar == ']') {
                    nextChar = stream.ReadCharOne();
                    break;
                }
                onEntry();

                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(stream, nextChar == ',' || nextChar == ']');
            };
            return true;
        }
        public override object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionValueAccessor, MemberAccessor memberAccessor) {
            if (ReadNull(stream)) return null;
            if (obj == null) obj = typeAccessor.Construct();
            if (typeAccessor.IsDictObj) {
                var dict = obj as IDictionary;
                return ReadObject(stream, name => {
                    var keyType = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[0]);
                    var valueType = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[1]);
                    object key = name;
                    if (keyType.Surrogate != null) key = keyType.Surrogate.ConvertFrom(this, key);
                    var value = Read(dict[key], stream, valueType, null);
                    dict[key] = value;
                }) ? obj : null;
            }

            return ReadCollection(stream,
                () => CollectionUtil.AddToCollection(obj, Read(null, stream, collectionValueAccessor, null)))
                    ? obj : null;
        }

        private bool ReadObject(Stream stream, Action<string> onEntry) {
            if (stream.ReadCharOne(true) != '{') {
                if (Config.Strict) throw new SerializationError();
                ReadToken(stream);
                return false;
            }
            stream.ReadCharOne();
            stream.ReadAllString(IsW);
            char nextChar = stream.ReadCharOne(true);
            if (nextChar == '}') stream.ReadCharOne();
            while (nextChar != '}') {
                stream.ReadAllString(IsW);
                var name = stringSer.Read(stream);
                stream.ReadAllString(IsW);
                CheckAndThrow(stream, stream.ReadCharOne() == ':');
                stream.ReadAllString(IsW);
                onEntry(name);
                stream.ReadAllString(IsW);
                nextChar = stream.ReadCharOne();
                CheckAndThrow(stream, nextChar == ',' || nextChar == '}');
            };
            return true;
        }
        public override object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor) {
            if (ReadNull(stream)) return null;
            if (obj == null) obj = typeAccessor.Construct();
            return ReadObject(stream, name => {
                var convertedName = Config.KeyConvert.From?.Invoke(name) ?? name;
                var childMember = typeAccessor.SerializedMembers.Values.FirstOrDefault(m => m.Info.Name == convertedName);
                if (childMember == null) {
                    CheckAndThrow(stream, !Config.Strict, $"Unknown field {name}");
                    ReadToken(stream);
                    return;
                }
                var value = Read(childMember.GetValue(obj), stream, childMember.TypeAccessor, childMember);
                childMember.SetValue(obj, value);
            }) ? obj : null;
        }

        public PrimitiveType PeekPrimitiveType(Stream stream) {
            stream.ReadAllString(IsW);
            var c = stream.ReadCharOne(true);
            if (c == '"') return PrimitiveType.String;
            if (c == 't' || c == 'f') return PrimitiveType.Bool;
            return PrimitiveType.Double;
        }

        private object ReadPrimitive(Stream stream) {
            return serializers[PeekPrimitiveType(stream)].Read(stream);
        }

        public override object ReadPrimitive(Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor) {
            if (ReadNull(stream)) return null;
            try {
                return Model.Coerce(typeAccessor, ReadPrimitive(stream));
            } catch (Exception e) {
                throw new SerializationError(null, e);
            }
        }

        bool ReadNull(Stream stream) {
            stream.ReadAllString(IsW);
            if (stream.ReadCharOne(true) == 'n') {
                CheckAndThrow(stream, stream.ReadChars(4) == "null");
                return true;
            }
            return false;
        }
        IEnumerable<(string key, object value, TypeAccessor type, MemberAccessor member)>
            getDictValues(IDictionary dict, TypeAccessor typeAccessor) {
            var keyType = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[0]);
            var valueType = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[1]);
            foreach (var key in dict.Keys) {
                var strKey = key;
                if (keyType.Surrogate != null) strKey = keyType.Surrogate.ConvertTo(this, strKey);
                yield return (strKey as string, dict[key], valueType, null);
            }
        }
        public override void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor) {
            if (obj == null)
                WritePrimitive(stream, null, null, null);
            else {
                if (typeAccessor.IsDictObj) {
                    SerializeObject(stream, getDictValues(obj as IDictionary, typeAccessor));
                    return;
                }
                stream.WriteString("[");
                bool first = true;
                foreach (var item in (IEnumerable)obj) {
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    Write(stream, item, collectionValueType, null);
                }
                stream.WriteString("]");
            }
        }
        bool IsEnumerableEmpty(IEnumerable enumerable) {
            foreach (var item in enumerable) return false;
            return true;
        }
        public void SerializeObject(Stream stream, IEnumerable<(string key, object value, TypeAccessor type, MemberAccessor member)> obj) {
            if (obj == null)
                WritePrimitive(stream, null, null, null);
            else {
                stream.WriteString("{");
                bool first = true;
                foreach (var (key, value, type, member) in obj) {
                    if ((member?.SkipNull ?? false) && value == null) continue;
                    if (value != null && (member?.SkipEmpty ?? false) && IsEnumerableEmpty((IEnumerable)value)) continue;
                    if (!first) stream.WriteString(", ");
                    else first = false;
                    stream.WriteString($"\"{key}\": ");
                    Write(stream, value, type, member);
                }
                stream.WriteString("}");
            }
        }

        public override void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor) {
            // Prevent circular references
            if (ObjectStack.Value == null) ObjectStack.Value = new Stack<object>();
            if (ObjectStack.Value.Contains(obj)) {
                WritePrimitive(stream, null, null, null);
                return;
            }
            using var objTracker = new ObjectTracker(this, obj);

            var objectSet = obj == null ? null : typeAccessor.SerializedMembers.Keys.Select(key => {
                var member = typeAccessor[key];
                return (Config.KeyConvert.To?.Invoke(key.Name) ?? key.Name, member.GetValue(obj), member.TypeAccessor, member);
            });
            SerializeObject(stream, objectSet);
        }

        public override void WritePrimitive(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor) {
            if (obj == null)
                stream.WriteString("null");
            else
                serializers[typeAccessor.TypeData.PrimitiveType].Write(obj, stream);
        }

        public override void WriteBlob(Stream stream, Blob obj, MemberAccessor memberAccessor) {
            if (obj?.Stream == null) {
                WritePrimitive(stream, null, null, null);
                return;
            }
            obj.Stream.CopyTo(stream);
        }

        public MarshallMethod PeekMarshallMethod(Stream stream) {
            stream.ReadAllString(IsW);
            var c = stream.ReadCharOne(true);
            // The 'n' is for null, return it as a null object
            if (c == '{' || c == 'n') return MarshallMethod.Object;
            if (c == '[') return MarshallMethod.Collection;
            return MarshallMethod.Primitive;
        }

        private void ReadToken(Stream stream) {
            if (ReadNull(stream)) return;
            var marshallMethod = PeekMarshallMethod(stream);
            switch (marshallMethod) {
                case MarshallMethod.Primitive:
                    ReadPrimitive(stream);
                    break;
                case MarshallMethod.Collection:
                    ReadCollection(stream, () => ReadToken(stream));
                    break;
                case MarshallMethod.Object:
                    ReadObject(stream, name => ReadToken(stream));
                    break;
                default:
                    break;
            }
        }

        public override Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor) {
            var substream = new SubStream(stream, stream.Length - stream.Position);
            ReadToken(substream);
            substream.SetLength(substream.Position);
            substream.Position = 0;
            var blob = obj ?? new Blob();
            blob.SetStream(substream);
            return blob;
        }
    }
}
