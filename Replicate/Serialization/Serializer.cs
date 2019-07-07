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
        public void Serialize<T>(TContext stream, T obj)
        {
            Serialize(stream, typeof(T), obj);
        }
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
            var marshalMethod = typeAccessor.TypeData.MarshalMethod;
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    SerializePrimitive(stream, obj, typeAccessor.Type);
                    break;
                case MarshalMethod.Collection:
                    var collectionValueType = Model.GetCollectionValueAccessor(typeAccessor.Type);
                    SerializeCollection(stream, obj, collectionValueType);
                    break;
                case MarshalMethod.Tuple:
                    SerializeTuple(stream, obj, typeAccessor);
                    break;
                case MarshalMethod.Object:
                    SerializeObject(stream, obj, typeAccessor);
                    break;
            }
        }
        public abstract void SerializePrimitive(TContext stream, object obj, Type type);
        public abstract void SerializeCollection(TContext stream, object obj, TypeAccessor collectionValueType);
        public abstract void SerializeObject(TContext stream, object obj, TypeAccessor typeAccessor);
        public abstract void SerializeTuple(TContext stream, object obj, TypeAccessor typeAccessor);
        public object Deserialize(Type type, TWireType wire) => Deserialize(type, GetContext(wire));
        public T Deserialize<T>(TContext stream)
        {
            return (T)Deserialize(null, stream, Model.GetTypeAccessor(typeof(T)), null);
        }
        public object Deserialize(Type type, TContext stream)
        {
            return Deserialize(null, stream, Model.GetTypeAccessor(type), null);
        }
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
            var type = typeAccessor.Type;
            switch (typeAccessor.TypeData.MarshalMethod)
            {
                case MarshalMethod.Primitive:
                    return DeserializePrimitive(stream, type);
                case MarshalMethod.Collection:
                    var collectionValueType = Model.GetCollectionValueAccessor(type);
                    return DeserializeCollection(obj, stream, type, collectionValueType);
                case MarshalMethod.Tuple:
                    return DeserializeTuple(stream, type, typeAccessor);
                case MarshalMethod.Object:
                    return DeserializeObject(obj, stream, type, typeAccessor);
                default:
                    return Default(type);
            }
        }
        protected static object Default(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
        public abstract object DeserializePrimitive(TContext stream, Type type);
        public abstract object DeserializeObject(object obj, TContext stream, Type type, TypeAccessor typeAccessor);
        public abstract object DeserializeCollection(object obj, TContext stream, Type type, TypeAccessor typeAccessor);
        public abstract object DeserializeTuple(TContext stream, Type type, TypeAccessor typeAccessor);
    }
}
