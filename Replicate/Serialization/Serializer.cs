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
        Dictionary<Type, IReplicateSerializer> primitiveSerializers = new Dictionary<Type, IReplicateSerializer>();
        public Serializer(ReplicationManager manager)
        {
            this.manager = manager;
            primitiveSerializers[typeof(byte)] = new IntSerializer();
            primitiveSerializers[typeof(short)] = new IntSerializer();
            primitiveSerializers[typeof(int)] = new IntSerializer();
            primitiveSerializers[typeof(long)] = new IntSerializer();
            primitiveSerializers[typeof(ushort)] = new IntSerializer();
            primitiveSerializers[typeof(uint)] = new IntSerializer();
            primitiveSerializers[typeof(ulong)] = new IntSerializer();
            primitiveSerializers[typeof(float)] = new FloatSerializer();
            primitiveSerializers[typeof(string)] = new StringSerializer();
        }
        public void Serialize(Stream stream, object obj)
        {
            Serialize(stream, obj, manager.Model[obj.GetType()]);
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
                    if (primitiveSerializers.ContainsKey(type))
                        marshalMethod = MarshalMethod.Primitive;
                    else if(type.GetInterface("ICollection`1") != null)
                    {
                        marshalMethod = MarshalMethod.Collection;
                    }
                    else
                        throw new InvalidOperationException(string.Format("Cannot serialize {0}", type.Name));
                }
                else
                {
                    marshalMethod = (typeData?.MarshalByReference ?? false) ? MarshalMethod.Reference : MarshalMethod.Value;
                }
            }
            Serialize(stream, obj, typeData, marshalMethod);
        }
        public void Serialize(Stream stream, object obj, TypeData typeData, MarshalMethod marshalMethod)
        {
            var writer = new BinaryWriter(stream);
            stream.WriteByte((byte)marshalMethod);
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    Type type = obj.GetType();
                    primitiveSerializers[type].Write(obj, writer);
                    break;
                case MarshalMethod.Collection:
                    var enumerable = (IEnumerable)obj;
                    var count = 0;
                    foreach (var item in enumerable)
                        count++;
                    writer.Write(count);
                    foreach (var item in (IEnumerable)obj)
                        Serialize(stream, item);
                    break;
                case MarshalMethod.Value:
                    writer.Write(typeData.ReplicatedMembers.Count);
                    for (int id = 0; id < typeData.ReplicatedMembers.Count; id++)
                    {
                        var member = typeData.ReplicatedMembers[id];
                        writer.Write((byte)id);
                        Serialize(stream, member.GetValue(obj), member.TypeData);
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
            TypeData typeData = manager.Model[type];
            return (T)Deserialize(null, stream, type, typeData);
        }
        public object Deserialize(object obj, Stream stream, Type type, TypeData typeData)
        {
            BinaryReader reader = new BinaryReader(stream);
            var marshalMethod = (MarshalMethod)stream.ReadByte();
            switch (marshalMethod)
            {
                case MarshalMethod.Primitive:
                    return Convert.ChangeType(primitiveSerializers[type].Read(reader), type);
                case MarshalMethod.Collection:
                    {
                        int count = reader.ReadInt32();
                        obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
                        var collectionType = type.GetInterface("ICollection`1").GetGenericArguments()[0];
                        var collectionTypeData = manager.Model[collectionType];
                        var addMeth = type.GetMethod("Add");
                        if (addMeth != null)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                addMeth.Invoke(obj, new object[] { Deserialize(null, stream, collectionType, collectionTypeData) });
                            }
                        }
                        else if(obj is Array)
                        {
                            var arr = obj as Array;
                            for (int i = 0; i < count; i++)
                            {
                                arr.SetValue(Deserialize(null, stream, collectionType, collectionTypeData), i);
                            }
                        }
                        return obj;
                    }
                case MarshalMethod.Value:
                    {
                        if (obj == null)
                            obj = Activator.CreateInstance(type);
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int id = reader.ReadByte();
                            if(typeData == null)
                            {

                            }
                            var member = typeData.ReplicatedMembers[id];
                            member.SetValue(obj, Deserialize(member.GetValue(obj), stream, member.MemberType, member.TypeData));
                        }
                        return obj;
                    }
                case MarshalMethod.Reference:
                    {
                        var id = (ReplicatedID)Deserialize(null, stream, typeof(ReplicatedID), manager.Model["ReplicatedID"]);
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
