using Replicate.Messages;
using Replicate.MetaData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class Serializer
    {
        ReplicationManager manager;
        ReplicationModel model;
        public Serializer(ReplicationManager manager)
            : this(manager.Model)
        {
            this.manager = manager;
        }
        public Serializer(ReplicationModel model)
        {
            this.model = model;
        }
        private Serializer() { }
        public void Serialize(Stream stream, object obj)
        {
            Serialize(stream, obj, model[obj.GetType()]);
        }
        /// TODO: Replicate members by reference or copy depending on <see cref="ReplicationPolicy"/>
        public void Serialize(Stream stream, object obj, TypeData typeData)
        {
            MarshalMethod marshalMethod = MarshalMethod.Null;
            if (obj != null)
            {
                Type type = obj.GetType();
                if (typeData == null)
                {
                    throw new InvalidOperationException(string.Format("Cannot serialize {0}", type.Name));
                }
                else
                {
                    marshalMethod = typeData.Policy.MarshalMethod;
                }
            }
            Serialize(stream, obj, typeData, marshalMethod);
        }
        public void Serialize(Stream stream, object obj, TypeData typeData, MarshalMethod marshalMethod)
        {
            if (marshalMethod == MarshalMethod.Reference && manager == null)
                marshalMethod = MarshalMethod.Value;
            stream.WriteByte((byte)marshalMethod);
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    typeData.Policy.Serializer.Write(obj, stream);
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
                case MarshalMethod.Value:
                    stream.Write(BitConverter.GetBytes(typeData.ReplicatedMembers.Count), 0, 4);
                    for (int id = 0; id < typeData.ReplicatedMembers.Count; id++)
                    {
                        var member = typeData.ReplicatedMembers[id];
                        stream.WriteByte((byte)id);
                        var memberTypeData = member.IsGenericParameter ?
                            model[obj.GetType().GetGenericArguments()[member.GenericParameterPosition]] : member.TypeData;
                        Serialize(stream, member.GetValue(obj), memberTypeData);
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
        public T Deserialize<T>(Stream stream)
        {
            Type type = typeof(T);
            return (T)Deserialize(null, stream, type);
        }
        public object Deserialize(object obj, Stream stream, Type type, TypeData typeData = null)
        {
            var marshalMethod = (MarshalMethod)stream.ReadByte();
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    if (typeData == null)
                        typeData = model[type];
                    return Convert.ChangeType(typeData.Policy.Serializer.Read(stream), type);
                case MarshalMethod.Collection:
                    {
                        int count = stream.ReadInt32();
                        obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
                        var collectionType = type.GetInterface("ICollection`1").GetGenericArguments()[0];
                        var collectionTypeData = model[collectionType];
                        if (obj is Array)
                        {
                            var arr = obj as Array;
                            for (int i = 0; i < count; i++)
                            {
                                arr.SetValue(Deserialize(null, stream, collectionType, collectionTypeData), i);
                            }
                        }
                        else
                        {
                            var addMeth = type.GetInterface("ICollection`1").GetMethod("Add");
                            for (int i = 0; i < count; i++)
                            {
                                addMeth.Invoke(obj, new object[] { Deserialize(null, stream, collectionType, collectionTypeData) });
                            }
                        }
                        return obj;
                    }
                case MarshalMethod.Value:
                    {
                        if (obj == null)
                            obj = Activator.CreateInstance(type);
                        int count = stream.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int id = stream.ReadByte();
                            if (typeData == null)
                                typeData = model[type];
                            var member = typeData.ReplicatedMembers[id];
                            if (member.IsGenericParameter)
                                member.SetValue(obj, Deserialize(member.GetValue(obj), stream, type.GetGenericArguments()[member.GenericParameterPosition]));
                            else
                                member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.MemberType, member.TypeData));
                        }
                        return obj;
                    }
                case MarshalMethod.Reference:
                    {
                        if (manager == null)
                            throw new InvalidOperationException("Cannot perform reference serialization without a ReplicationManager");
                        var id = (ReplicatedID)Deserialize(null, stream, typeof(ReplicatedID), model[typeof(ReplicatedID)]);
                        return manager.idLookup[id].replicated;
                    }
                case MarshalMethod.Null:
                default:
                    if (type.IsValueType)
                        return Activator.CreateInstance(type);
                    return null;
            }
        }
    }
}
