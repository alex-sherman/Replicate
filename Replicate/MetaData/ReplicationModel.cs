using Replicate.Messages;
using Replicate.Serialization;
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
        Value = 2,
        Reference = 3,
        Collection = 4,
        Tuple = 5,
    }
    public class ReplicationModel
    {
        public bool AutoAddType { get; set; } = true;
        public static ReplicationModel Default { get; } = new ReplicationModel();
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

        private TypeData GetTypeData(Type type, bool autoAddType)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            if (typeLookup.ContainsKey(type))
                return typeLookup[type];
            if (type.GetInterface("ICollection`1") != null)
                return typeLookup[typeof(ICollection<>)];
            if (autoAddType)
                return Add(type);
            return null;
        }
        private ushort GetIndex(Type type)
        {
            return (ushort)typeLookup.Values.ToList().IndexOf(GetTypeData(type, false));
        }
        public TypeID GetID(Type type)
        {
            var output = new TypeID()
            {
                id = GetIndex(type)
            };
            if (type.IsGenericType)
                output.subtypes = type.GetGenericArguments().Select(t => GetID(t)).ToArray();
            return output;
        }
        public TypeAccessor this[TypeID typeID]
        {
            get
            {
                Type type = typeLookup.Keys.Skip(typeID.id).First();
                // TODO
                if(type.IsGenericTypeDefinition)
                {

                }
                return this[type];
            }
        }
        public TypeAccessor this[Type type]
        {
            get { return GetTypeData(type, AutoAddType).GetAccessor(type, this); }
        }
        public TypeData this[string typeName]
        {
            get { return stringLookup[typeName]; }
        }
        public TypeData Add(Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            var output = new TypeData(type);
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
                member.TypeData = GetTypeData(member.MemberType, AutoAddType && member.MemberType.GetCustomAttribute<ReplicateAttribute>() != null);
            }
        }
    }
}
