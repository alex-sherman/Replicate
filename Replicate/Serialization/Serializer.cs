using Replicate.Messages;
using Replicate.MetaData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public abstract class Serializer : IReplicateSerializer
    {
        public ReplicationModel Model { get; private set; }
        public Serializer(ReplicationModel model)
        {
            Model = model ?? ReplicationModel.Default;
        }
        private Serializer() { }
        public Stream Serialize(Type type, object obj, Stream stream)
        {
            Write(stream, obj, Model.GetTypeAccessor(type), null);
            return stream;
        }
        public void Write(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            var surrogateAccessor = memberAccessor?.Surrogate ?? typeAccessor.Surrogate;
            if (surrogateAccessor != null)
            {
                obj = surrogateAccessor.ConvertTo(obj);
                typeAccessor = surrogateAccessor.TypeAccessor;
            }
            var marshalMethod = typeAccessor.TypeData.MarshallMethod;
            switch (marshalMethod)
            {
                case MarshallMethod.Primitive:
                    WritePrimitive(stream, obj, typeAccessor, memberAccessor);
                    break;
                case MarshallMethod.Collection:
                    var collectionValueType = Model.GetCollectionValueAccessor(typeAccessor.Type);
                    WriteCollection(stream, obj, typeAccessor, collectionValueType, memberAccessor);
                    break;
                case MarshallMethod.Object:
                    WriteObject(stream, obj, typeAccessor, memberAccessor);
                    break;
                case MarshallMethod.Blob:
                    WriteBlob(stream, obj as Blob, memberAccessor);
                    break;
            }
        }
        public abstract void WriteBlob(Stream stream, Blob blob, MemberAccessor memberAccessor);
        public abstract void WritePrimitive(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor);
        public abstract void WriteCollection(Stream stream, object obj, TypeAccessor typeAccessor, TypeAccessor collectionValueType, MemberAccessor memberAccessor);
        public abstract void WriteObject(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor);
        public T Deserialize<T>(Stream wireValue) => (T)Deserialize(typeof(T), wireValue, null);
        public object Deserialize(Type type, Stream wire, object existing = null)
             => Read(existing, wire, Model.GetTypeAccessor(type), null);
        public object Read(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            var surrogateAccessor = memberAccessor?.Surrogate ?? typeAccessor?.Surrogate;
            if (surrogateAccessor != null)
            {
                obj = null;
                typeAccessor = surrogateAccessor?.TypeAccessor;
            }
            obj = _read(obj, stream, typeAccessor, memberAccessor);
            return surrogateAccessor == null ? obj : surrogateAccessor.ConvertFrom(obj);
        }
        private object _read(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            switch (typeAccessor.TypeData.MarshallMethod)
            {
                case MarshallMethod.Primitive:
                    return ReadPrimitive(stream, typeAccessor, memberAccessor);
                case MarshallMethod.Collection:
                    return ReadCollection(obj, stream, typeAccessor, Model.GetCollectionValueAccessor(typeAccessor.Type), memberAccessor);
                case MarshallMethod.Object:
                    return ReadObject(obj, stream, typeAccessor, memberAccessor);
                case MarshallMethod.Blob:
                    return ReadBlob((Blob)obj, stream, typeAccessor, memberAccessor);
                default:
                    return Default(typeAccessor.Type);
            }
        }
        protected static object Default(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
        public abstract Blob ReadBlob(Blob obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor);
        public abstract object ReadPrimitive(Stream stream, TypeAccessor type, MemberAccessor memberAccessor);
        public abstract object ReadObject(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor);
        public abstract object ReadCollection(object obj, Stream stream, TypeAccessor typeAccessor, TypeAccessor collectionAccessor, MemberAccessor memberAccessor);
    }
}
