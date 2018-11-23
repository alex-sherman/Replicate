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
    public abstract class Serializer
    {
        public delegate Type DynamicSurrogate(object obj, MemberAccessor memberAccessor);
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
                var castOp = surrogateAccessor.Type.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                typeAccessor = surrogateAccessor;
                var surrogate = castOp.Invoke(null, new object[] { obj });
                obj = surrogate;
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
            Type type = typeof(T);
            return (T)Deserialize(null, stream, Model.GetTypeAccessor(type), null);
        }
        public object Deserialize(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            MethodInfo castOp = null;
            var surrogateAccessor = memberAccessor?.Surrogate ?? typeAccessor.Surrogate;
            if (surrogateAccessor != null)
            {
                var invCastOp = surrogateAccessor.Type.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                typeAccessor = surrogateAccessor;
                castOp = typeAccessor.Type.GetMethod("op_Implicit", new Type[] { invCastOp.ReturnType });
                obj = null;
            }
            obj = DeserializeRaw(obj, stream, typeAccessor);
            return castOp?.Invoke(null, new object[] { obj }) ?? obj;

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
        protected static object FillCollection(object obj, Type type, List<object> values)
        {
            var count = values.Count;
            if (obj == null)
                obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
            else
            {
                var clearMeth = type.GetInterface("ICollection`1").GetMethod("Clear");
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
                var addMeth = type.GetInterface("ICollection`1").GetMethod("Add");
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
