﻿using Replicate.Serialization;
using System;
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
        Null = 0,
        Primitive = 1,
        Object = 2,
        Collection = 3,
        Tuple = 4,
        Reference = -1,
    }
    public class ReplicationModel
    {
        public static ReplicationModel Default { get; } = new ReplicationModel();
        Dictionary<Type, TypeAccessor> typeAccessorLookup = new Dictionary<Type, TypeAccessor>();
        Dictionary<Type, TypeData> typeLookup = new Dictionary<Type, TypeData>();
        Dictionary<string, TypeData> stringLookup = new Dictionary<string, TypeData>();
        public IReplicateSerializer IntSerializer = new IntSerializer();
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
            Add(typeof(ICollection<>)).Policy.MarshalMethod = MarshalMethod.Collection;
            var kvpTD = Add(typeof(KeyValuePair<,>));
            kvpTD.Policy.MarshalMethod = MarshalMethod.Tuple;
            kvpTD.AddMember("Key");
            kvpTD.AddMember("Value");
        }
        public TypeAccessor GetTypeAccessor(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new InvalidOperationException("Cannot create a type accessor for a generic type definition");
            if (!typeAccessorLookup.TryGetValue(type, out TypeAccessor typeAcessor))
                typeAcessor = typeAccessorLookup[type] = new TypeAccessor(GetTypeData(type), type, this);
            return typeAcessor;
        }
        public TypeData GetTypeData(Type type, bool autoAddType = true)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            if (typeLookup.TryGetValue(type, out TypeData td))
                return td;
            if (autoAddType)
                return Add(type);
            return null;
        }
        public TypeData this[Type type]
        {
            get { return GetTypeData(type); }
        }
        public TypeData Add(Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            var output = new TypeData(type, this);
            Compile(output);
            typeLookup.Add(type, output);
            stringLookup.Add(type.FullName, output);
            return output;
        }
        public void LoadTypes(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes().Where(asmType => asmType.GetCustomAttribute<ReplicateAttribute>() != null))
            {
                Add(type);
            }
        }
        private void Compile(TypeData typeData)
        {
            foreach (var member in typeData.ReplicatedMembers)
            {
                member.TypeData = GetTypeData(member.MemberType, member.MemberType.GetCustomAttribute<ReplicateAttribute>() != null);
            }
        }
    }
}
