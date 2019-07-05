﻿using Replicate.Messages;
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
    public abstract class Serializer
    {
        public ReplicationModel Model { get; private set; }
        public Serializer(ReplicationModel model)
        {
            Model = model;
        }
        private Serializer() { }
        public void Serialize<T>(Stream stream, T obj)
        {
            Serialize(stream, typeof(T), obj);
        }
        public void Serialize(Stream stream, Type type, object obj)
        {
            Serialize(stream, obj, Model.GetTypeAccessor(type), null);
        }
        public void Serialize(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
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
        public abstract void SerializePrimitive(Stream stream, object obj, Type type);
        public abstract void SerializeCollection(Stream stream, object obj, TypeAccessor collectionValueType);
        public abstract void SerializeObject(Stream stream, object obj, TypeAccessor typeAccessor);
        public abstract void SerializeTuple(Stream stream, object obj, TypeAccessor typeAccessor);
        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(null, stream, Model.GetTypeAccessor(typeof(T)), null);
        }
        public object Deserialize(Stream stream, Type type)
        {
            return Deserialize(null, stream, Model.GetTypeAccessor(type), null);
        }
        public object Deserialize(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
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
        private object DeserializeRaw(object obj, Stream stream, TypeAccessor typeAccessor)
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
        public static object FillCollection(object obj, Type type, List<object> values)
        {
            var count = values.Count;

            if (type.IsSameGeneric(typeof(IEnumerable<>)) || type.IsSameGeneric(typeof(ICollection<>)))
            {
                type = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                obj = null;
            }
            var collectionType = type.GetInterface("ICollection`1");
            if (obj is Array || obj == null)
                obj = Activator.CreateInstance(type, count);
            else
            {
                var clearMeth = collectionType.GetMethod("Clear");
                clearMeth.Invoke(obj, new object[] { });
            }
            if (obj is Array)
            {
                var arr = obj as Array;
                for (int i = 0; i < count; i++)
                    arr.SetValue(values[i], i);
            }
            else
            {
                var addMeth = collectionType.GetMethod("Add");
                foreach(var value in values)
                    addMeth.Invoke(obj, new object[] { value });
            }
            return obj;
        }
        public abstract object DeserializePrimitive(Stream stream, Type type);
        public abstract object DeserializeObject(object obj, Stream stream, Type type, TypeAccessor typeAccessor);
        public abstract object DeserializeCollection(object obj, Stream stream, Type type, TypeAccessor typeAccessor);
        public abstract object DeserializeTuple(Stream stream, Type type, TypeAccessor typeAccessor);
    }
}
