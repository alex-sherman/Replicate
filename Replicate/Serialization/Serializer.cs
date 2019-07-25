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
            Write(context, obj, Model.GetTypeAccessor(type), null);
            return GetWireValue(context);
        }
        public TWireType Serialize<T>(T obj) => Serialize(typeof(T), obj);
        public void Write(TContext stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
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
                    WritePrimitive(stream, obj, typeAccessor);
                    break;
                case MarshallMethod.Collection:
                    var collectionValueType = Model.GetCollectionValueAccessor(typeAccessor.Type);
                    WriteCollection(stream, obj, collectionValueType);
                    break;
                case MarshallMethod.Object:
                    WriteObject(stream, obj, typeAccessor);
                    break;
                case MarshallMethod.Blob:
                    WriteBlob(stream, obj as Blob ?? Blob.None, memberAccessor);
                    break;
            }
        }
        public abstract void WriteBlob(TContext stream, Blob blob, MemberAccessor memberAccessor);
        public abstract void WritePrimitive(TContext stream, object obj, TypeAccessor type);
        public abstract void WriteCollection(TContext stream, object obj, TypeAccessor collectionValueType);
        public abstract void WriteObject(TContext stream, object obj, TypeAccessor typeAccessor);
        public T Deserialize<T>(TWireType wireValue) => (T)Deserialize(typeof(T), wireValue, null);
        public object Deserialize(Type type, TWireType wire, object existing = null)
             => Read(existing, GetContext(wire), Model.GetTypeAccessor(type), null);
        public object Read(object obj, TContext stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
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
        private object _read(object obj, TContext stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            switch (typeAccessor.TypeData.MarshallMethod)
            {
                case MarshallMethod.Primitive:
                    return ReadPrimitive(stream, typeAccessor);
                case MarshallMethod.Collection:
                    return ReadCollection(obj, stream, typeAccessor, Model.GetCollectionValueAccessor(typeAccessor.Type));
                case MarshallMethod.Object:
                    return ReadObject(obj, stream, typeAccessor);
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
        public abstract Blob ReadBlob(Blob obj, TContext stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor);
        public abstract object ReadPrimitive(TContext stream, TypeAccessor type);
        public abstract object ReadObject(object obj, TContext stream, TypeAccessor typeAccessor);
        public abstract object ReadCollection(object obj, TContext stream, TypeAccessor typeAccessor, TypeAccessor collectionAccessor);
    }
}
