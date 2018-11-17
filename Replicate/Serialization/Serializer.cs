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
        public void Serialize(Stream stream, object obj)
        {
            Serialize(stream, obj, Model.GetTypeAccessor(obj.GetType()), null);
        }
        public void Serialize(Stream stream, object obj, TypeAccessor typeAccessor, MemberAccessor memberAccessor, DynamicSurrogate dynamicSurrogate = null)
        {
            MarshalMethod marshalMethod = MarshalMethod.Null;
            if (obj != null && typeAccessor == null)
                throw new InvalidOperationException(string.Format("Cannot serialize {0}", obj.GetType().Name));

            var surType = dynamicSurrogate?.Invoke(typeAccessor, memberAccessor) ?? typeAccessor.TypeData.Surrogate?.Type;
            if (surType != null)
            {
                var castOp = surType.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                var surrogate = castOp.Invoke(null, new object[] { obj });
                typeAccessor = Model.GetTypeAccessor(surType);
                obj = surrogate;
            }
            marshalMethod = obj == null ? MarshalMethod.Null : typeAccessor.TypeData.MarshalMethod;
            stream.WriteByte((byte)marshalMethod);
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    SerializePrimitive(stream, obj, typeAccessor);
                    break;
                case MarshalMethod.Collection:
                    var enumerable = (IEnumerable)obj;
                    var count = 0;
                    foreach (var item in enumerable)
                        count++;
                    stream.WriteInt32(count);
                    foreach (var item in (IEnumerable)obj)
                        Serialize(stream, item);
                    break;
                case MarshalMethod.Tuple:
                    foreach (var member in typeAccessor.MemberAccessors)
                    {
                        Serialize(stream, member.GetValue(obj), member.TypeAccessor, member, dynamicSurrogate);
                    }
                    break;
                case MarshalMethod.Object:
                    stream.Write(BitConverter.GetBytes(typeAccessor.MemberAccessors.Length), 0, 4);
                    for (int id = 0; id < typeAccessor.MemberAccessors.Length; id++)
                    {
                        var member = typeAccessor.MemberAccessors[id];
                        stream.WriteByte((byte)id);
                        Serialize(stream, member.GetValue(obj), member.TypeAccessor, member, dynamicSurrogate);
                    }
                    break;
                case MarshalMethod.Null:
                default:
                    break;
            }
        }
        public abstract void SerializePrimitive(Stream stream, object obj, TypeAccessor typeData);
        public T Deserialize<T>(Stream stream)
        {
            Type type = typeof(T);
            return (T)Deserialize(null, stream, Model.GetTypeAccessor(type), null);
        }
        public object Deserialize(object obj, Stream stream, TypeAccessor typeAccessor, MemberAccessor memberAccessor, DynamicSurrogate dynamicSurrogate = null)
        {
            MethodInfo castOp = null;
            var surType = dynamicSurrogate?.Invoke(obj, memberAccessor) ?? typeAccessor.TypeData.Surrogate?.Type;
            if (surType != null)
            {
                var invCastOp = surType.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                castOp = surType.GetMethod("op_Implicit", new Type[] { invCastOp.ReturnType });
                typeAccessor = Model.GetTypeAccessor(surType);
                obj = null;
            }
            obj = DeserializeRaw(obj, stream, typeAccessor, dynamicSurrogate);
            return castOp?.Invoke(null, new object[] { obj }) ?? obj;

        }
        private object DeserializeRaw(object obj, Stream stream, TypeAccessor typeAccessor, DynamicSurrogate dynamicSurrogate)
        {
            var type = typeAccessor.Type;
            var marshalMethod = (MarshalMethod)stream.ReadByte();
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    return DeserializePrimitive(stream, type, typeAccessor);
                case MarshalMethod.Collection:
                    {
                        int count = stream.ReadInt32();
                        if (obj == null)
                            obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
                        else
                        {
                            var clearMeth = type.GetInterface("ICollection`1").GetMethod("Clear");
                            clearMeth.Invoke(obj, new object[] { });
                        }
                        var collectionType = type.GetInterface("ICollection`1").GetGenericArguments()[0];
                        var collectionTypeAccessor = Model.GetTypeAccessor(collectionType);
                        if (obj is Array)
                        {
                            var arr = obj as Array;
                            for (int i = 0; i < count; i++)
                            {
                                arr.SetValue(Deserialize(null, stream, collectionTypeAccessor, null, dynamicSurrogate), i);
                            }
                        }
                        else
                        {
                            var addMeth = type.GetInterface("ICollection`1").GetMethod("Add");
                            for (int i = 0; i < count; i++)
                            {
                                addMeth.Invoke(obj, new object[] { Deserialize(null, stream, collectionTypeAccessor, null, dynamicSurrogate) });
                            }
                        }
                        return obj;
                    }
                case MarshalMethod.Tuple:
                    List<object> parameters = new List<object>();
                    List<Type> paramTypes = new List<Type>();
                    foreach (var member in typeAccessor.MemberAccessors)
                    {
                        paramTypes.Add(member.Type);
                        parameters.Add(Deserialize(null, stream, member.TypeAccessor, member, dynamicSurrogate));
                    }
                    return type.GetConstructor(paramTypes.ToArray()).Invoke(parameters.ToArray());
                case MarshalMethod.Object:
                    {
                        if (obj == null)
                            obj = Activator.CreateInstance(type);
                        int count = stream.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int id = stream.ReadByte();
                            var member = typeAccessor.MemberAccessors[id];
                            member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.TypeAccessor, member, dynamicSurrogate));
                        }
                        return obj;
                    }
                case MarshalMethod.Null:
                default:
                    if (type.IsValueType)
                        return Activator.CreateInstance(type);
                    return null;
            }
        }
        public abstract object DeserializePrimitive(Stream stream, Type type, TypeAccessor typeData);
    }
}
