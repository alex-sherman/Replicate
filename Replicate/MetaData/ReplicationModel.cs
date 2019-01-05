﻿using Replicate.Messages;
using Replicate.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public enum MarshalMethod
    {
        Primitive = 0,
        Object = 1,
        Collection = 2,
        Tuple = 3,
    }
    public class ReplicationModel : IEnumerable<TypeData>
    {
        public static ReplicationModel Default { get; } = new ReplicationModel();
        Dictionary<Type, TypeAccessor> typeAccessorLookup = new Dictionary<Type, TypeAccessor>();
        Dictionary<Type, TypeData> typeLookup = new Dictionary<Type, TypeData>();
        Dictionary<string, TypeData> stringLookup = new Dictionary<string, TypeData>();
        List<Type> typeIndex;
        public bool DictionaryAsObject;
        public ReplicationModel()
        {
            Add(typeof(byte));
            Add(typeof(short));
            Add(typeof(int));
            Add(typeof(long));
            Add(typeof(ushort));
            Add(typeof(uint));
            Add(typeof(ulong));
            Add(typeof(float));
            Add(typeof(string));
            Add(typeof(Dictionary<,>));
            Add(typeof(List<>));
            Add(typeof(ICollection<>));
            Add(typeof(IEnumerable<>));
            Add(typeof(Nullable<>));
            var kvpTD = Add(typeof(KeyValuePair<,>));
            kvpTD.MarshalMethod = MarshalMethod.Tuple;
            kvpTD.AddMember("Key");
            kvpTD.AddMember("Value");
            Add(typeof(TypedValue));
            LoadTypes(Assembly.GetCallingAssembly());
            typeIndex = typeLookup.Values.OrderBy(td => td.Name).Select(td => td.Type).ToList();
        }

        public IRepNode GetRepNode(object backing, TypeAccessor typeAccessor = null, MemberAccessor memberAccessor = null)
        {
            typeAccessor = typeAccessor ?? GetTypeAccessor(backing.GetType());
            if(DictionaryAsObject && typeAccessor.Type.IsSameGeneric(typeof(Dictionary<,>))
                && typeAccessor.Type.GetGenericArguments()[0] == typeof(string))
            {
                var childType = typeAccessor.Type.GetGenericArguments()[1];
                var dictObjType = typeof(RepDictObject<>).MakeGenericType(childType);
                return (IRepNode)Activator.CreateInstance(dictObjType, backing, this);
            }

            var output = new RepBackedNode(backing, typeAccessor, memberAccessor, this);
            var surrogate = memberAccessor?.Surrogate ?? typeAccessor.Surrogate;
            if (surrogate != null)
            {
                var castToOp = surrogate.Type.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                output.ConvertToSurrogate = obj => castToOp.Invoke(null, new[] { obj });

                var castFromOp = surrogate.Type.GetMethod("op_Implicit", new Type[] { surrogate.Type });
                output.ConvertFromSurrogate = obj => castFromOp.Invoke(null, new[] { obj });
                output.TypeAccessor = surrogate;
            }
            return output;
        }

        public TypeID GetID(Type type)
        {
            var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var output = new TypeID()
            {
                id = (ushort)typeIndex.IndexOf(genericType)
            };
            if (type.IsGenericType)
                output.subtypes = type.GetGenericArguments().Select(t => GetID(t)).ToArray();
            return output;
        }
        public Type GetType(TypeID typeID)
        {
            Type type = typeIndex[typeID.id];
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(typeID.subtypes.Select(subType => GetType(subType)).ToArray());
            }
            return type;
        }
        public TypeAccessor GetTypeAccessor(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new InvalidOperationException("Cannot create a type accessor for a generic type definition");
            if (!typeAccessorLookup.TryGetValue(type, out TypeAccessor typeAccessor))
            {
                var typeData = GetTypeData(type);
                if (typeData == null)
                    throw new InvalidOperationException(string.Format("The type {0} has not been added to the replication model", type.FullName));
                typeAccessor = typeAccessorLookup[type] = new TypeAccessor(typeData, type);
                typeAccessor.InitializeMembers();
            }
            return typeAccessor;
        }
        public TypeAccessor GetCollectionValueAccessor(Type collectionType)
        {
            if (!collectionType.IsGenericType || collectionType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                collectionType = collectionType.GetInterface("ICollection`1");
            return GetTypeAccessor(collectionType.GetGenericArguments()[0]);
        }
        public TypeData GetTypeData(Type type, bool autoAddType = true)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            if (typeLookup.TryGetValue(type, out TypeData td))
                return td;
            if (autoAddType)
                return Add(type);
            if (type.GetInterface("ICollection`1") != null)
                return typeLookup[typeof(ICollection<>)];
            if (type.GetInterface("IEnumerable`1") != null)
                return typeLookup[typeof(IEnumerable<>)];
            return null;
        }
        public TypeData this[Type type]
        {
            get { return GetTypeData(type); }
        }
        public TypeData Add(Type type)
        {
            if (type.IsNotPublic)
                throw new InvalidOperationException("Cannot add a non public type to the replication model");
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            if (typeLookup.TryGetValue(type, out TypeData typeData))
                return typeData;
            var output = new TypeData(type, this);
            typeLookup.Add(type, output);
            stringLookup.Add(type.FullName, output);
            return output;
        }
        static bool IsReplicateType(Type type)
        {
            return type.GetCustomAttribute<ReplicateTypeAttribute>() != null;
        }
        public void LoadTypes(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes().Where(IsReplicateType))
                Add(type);
        }

        public IEnumerator<TypeData> GetEnumerator() => typeLookup.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
