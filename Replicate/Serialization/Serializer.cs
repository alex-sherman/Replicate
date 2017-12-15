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
        ReplicationModel model;
        public Serializer(ReplicationModel model)
        {
            this.model = model;
        }
        private Serializer() { }
        public void Serialize(Stream stream, object obj)
        {
            Serialize(stream, obj, null);
        }
        public void Serialize(Stream stream, object obj, ReplicationManager manager)
        {
            Serialize(stream, obj, manager, model.GetTypeAccessor(obj.GetType()));
        }
        /// TODO: Replicate members by reference or copy depending on <see cref="ReplicationPolicy"/>
        public void Serialize(Stream stream, object obj, ReplicationManager manager, TypeAccessor typeAccessor)
        {
            MarshalMethod marshalMethod = MarshalMethod.Null;
            if (obj != null)
            {
                if (typeAccessor == null)
                    throw new InvalidOperationException(string.Format("Cannot serialize {0}", obj.GetType().Name));

                if (typeAccessor.TypeData.Surrogate != null)
                {
                    var surType = typeAccessor.TypeData.Surrogate.Type;
                    var castOp = surType.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                    var surrogate = castOp.Invoke(null, new object[] { obj });
                    typeAccessor = typeAccessor.TypeData.Surrogate;
                    obj = surrogate;
                }
                marshalMethod = typeAccessor.TypeData.Policy.MarshalMethod;
            }
            Serialize(stream, obj, manager, typeAccessor, marshalMethod);
        }
        public void Serialize(Stream stream, object obj, ReplicationManager manager, TypeAccessor typeAccessor, MarshalMethod marshalMethod)
        {
            if (marshalMethod == MarshalMethod.Reference && manager == null)
                marshalMethod = MarshalMethod.Value;
            stream.WriteByte((byte)marshalMethod);
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    SerializePrimitive(stream, obj, typeAccessor);
                    return;
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
                        Serialize(stream, member.GetValue(obj), manager, member.TypeAccessor);
                    }
                    break;
                case MarshalMethod.Value:
                    stream.Write(BitConverter.GetBytes(typeAccessor.MemberAccessors.Length), 0, 4);
                    for (int id = 0; id < typeAccessor.MemberAccessors.Length; id++)
                    {
                        var member = typeAccessor.MemberAccessors[id];
                        stream.WriteByte((byte)id);
                        Serialize(stream, member.GetValue(obj), manager, member.TypeAccessor);
                    }
                    break;
                case MarshalMethod.Reference:
                    Serialize(stream, manager.objectLookup[obj].id);
                    break;
                case MarshalMethod.Null:
                default:
                    break;
            }
        }
        public abstract void SerializePrimitive(Stream stream, object obj, TypeAccessor typeData);
        public T Deserialize<T>(Stream stream, ReplicationManager manager = null)
        {
            Type type = typeof(T);
            return (T)Deserialize(null, stream, type, manager);
        }
        public object Deserialize(object obj, Stream stream, Type type, ReplicationManager manager, TypeAccessor typeAccessor = null)
        {
            if (typeAccessor == null)
                typeAccessor = model.GetTypeAccessor(type);
            MethodInfo castOp = null;
            if (typeAccessor.TypeData.Surrogate != null)
            {
                var surType = typeAccessor.TypeData.Surrogate.Type;
                var invCastOp = surType.GetMethod("op_Implicit", new Type[] { type });
                castOp = surType.GetMethod("op_Implicit", new Type[] { invCastOp.ReturnType });
                typeAccessor = typeAccessor.TypeData.Surrogate;
            }
            obj = DeserializeRaw(obj, stream, manager, typeAccessor);
            return castOp?.Invoke(null, new object[] { obj }) ?? obj;

        }
        public object DeserializeRaw(object obj, Stream stream, ReplicationManager manager, TypeAccessor typeAccessor)
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
                        obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
                        var collectionType = type.GetInterface("ICollection`1").GetGenericArguments()[0];
                        var collectionTypeAccessor = model.GetTypeAccessor(collectionType);
                        if (obj is Array)
                        {
                            var arr = obj as Array;
                            for (int i = 0; i < count; i++)
                            {
                                arr.SetValue(Deserialize(null, stream, collectionType, manager, collectionTypeAccessor), i);
                            }
                        }
                        else
                        {
                            var addMeth = type.GetInterface("ICollection`1").GetMethod("Add");
                            for (int i = 0; i < count; i++)
                            {
                                addMeth.Invoke(obj, new object[] { Deserialize(null, stream, collectionType, manager, collectionTypeAccessor) });
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
                        parameters.Add(Deserialize(null, stream, member.Type, manager, member.TypeAccessor));
                    }
                    return type.GetConstructor(paramTypes.ToArray()).Invoke(parameters.ToArray());
                case MarshalMethod.Value:
                    {
                        if (obj == null)
                            obj = Activator.CreateInstance(type);
                        int count = stream.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int id = stream.ReadByte();
                            var member = typeAccessor.MemberAccessors[id];
                            member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.Type, manager, member.TypeAccessor));
                        }
                        return obj;
                    }
                case MarshalMethod.Reference:
                    {
                        if (manager == null)
                            throw new InvalidOperationException("Cannot perform reference serialization without a ReplicationManager");
                        var id = (ReplicatedID)Deserialize(null, stream, typeof(ReplicatedID), manager, model.GetTypeAccessor(typeof(ReplicatedID)));
                        return manager.idLookup[id].replicated;
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
