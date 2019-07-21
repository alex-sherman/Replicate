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
    public abstract class Serializer<TContext, TWireType> : IReplicateSerializer<TWireType>
    {
        public ReplicationModel Model { get; private set; }
        public Serializer(ReplicationModel model)
        {
            Model = model;
        }
        private Serializer() { }
        public abstract TContext GetContext(TWireType wireValue);
        public abstract TWireType GetWireValue(TContext context);
        public TWireType Serialize(Type type, object obj)
        {
            var context = GetContext(default(TWireType));
            Serialize(context, type, obj);
            return GetWireValue(context);
        }
        public TWireType Serialize<T>(T obj) => Serialize(typeof(T), obj);
        public void Serialize(TContext stream, Type type, object obj)
        {
            Serialize(stream, obj, Model.GetTypeAccessor(type), null);
        }
        public void Serialize(TContext stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            if (obj != null && typeAccessor == null)
                throw new InvalidOperationException(string.Format("Cannot serialize {0}", obj.GetType().Name));

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
                    SerializePrimitive(stream, obj, typeAccessor);
                    break;
                case MarshallMethod.Collection:
                    var collectionValueType = Model.GetCollectionValueAccessor(typeAccessor.Type);
                    SerializeCollection(stream, obj, collectionValueType);
                    break;
                case MarshallMethod.Tuple:
                    SerializeTuple(stream, obj, typeAccessor);
                    break;
                case MarshallMethod.Object:
                    SerializeObject(stream, obj, typeAccessor);
                    break;
            }
        }
        public abstract void SerializePrimitive(TContext stream, object obj, TypeAccessor type);
        public abstract void SerializeCollection(TContext stream, object obj, TypeAccessor collectionValueType);
        public abstract void SerializeObject(TContext stream, object obj, TypeAccessor typeAccessor);
        public abstract void SerializeTuple(TContext stream, object obj, TypeAccessor typeAccessor);
        public T Deserialize<T>(TWireType wireValue) => (T)Deserialize(typeof(T), wireValue);
        public object Deserialize(Type type, TWireType wire) => Deserialize(null, GetContext(wire), Model.GetTypeAccessor(type), null);
        public object Deserialize(object obj, TContext stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            var surrogateAccessor = memberAccessor?.Surrogate ?? typeAccessor.Surrogate;
            if (surrogateAccessor != null)
            {
                obj = null;
                typeAccessor = surrogateAccessor?.TypeAccessor;
            }
            obj = DeserializeRaw(obj, stream, typeAccessor);
            return surrogateAccessor == null ? obj : surrogateAccessor.ConvertFrom(obj);
        }
        private object DeserializeRaw(object obj, TContext stream, TypeAccessor typeAccessor)
        {
            switch (typeAccessor.TypeData.MarshallMethod)
            {
                case MarshallMethod.Primitive:
                    return DeserializePrimitive(stream, typeAccessor);
                case MarshallMethod.Collection:
                    return DeserializeCollection(obj, stream, typeAccessor, Model.GetCollectionValueAccessor(typeAccessor.Type));
                case MarshallMethod.Tuple:
                    return DeserializeTuple(stream, typeAccessor);
                case MarshallMethod.Object:
                    return DeserializeObject(obj, stream, typeAccessor);
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
        public abstract object DeserializePrimitive(TContext stream, TypeAccessor type);
        public abstract object DeserializeObject(object obj, TContext stream, TypeAccessor typeAccessor);
        public abstract object DeserializeCollection(object obj, TContext stream, TypeAccessor typeAccessor, TypeAccessor collectionAccessor);
        public abstract object DeserializeTuple(TContext stream, TypeAccessor typeAccessor);
    }
}
