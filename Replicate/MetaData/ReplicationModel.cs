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
            Add(typeof(byte)).Policy.Serializer = IntSerializer;
            Add(typeof(short)).Policy.Serializer = IntSerializer;
            Add(typeof(int)).Policy.Serializer = IntSerializer;
            Add(typeof(long)).Policy.Serializer = IntSerializer;
            Add(typeof(ushort)).Policy.Serializer = IntSerializer;
            Add(typeof(uint)).Policy.Serializer = IntSerializer;
            Add(typeof(ulong)).Policy.Serializer = IntSerializer;
            Add(typeof(float)).Policy.Serializer = new FloatSerializer();
            Add(typeof(string)).Policy.Serializer = new StringSerializer();
            Add(typeof(List<>)).Policy.MarshalMethod = MarshalMethod.Collection;
            var kvpTD = Add(typeof(KeyValuePair<,>));
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
                return typeLookup[typeof(List<>)];
            if (autoAddType)
                return Add(type);
            return null;
        }
        public TypeData this[Type type]
        {
            get { return GetTypeData(type, AutoAddType); }
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
            foreach (var member in typeData.ReplicatedMembers.Where(m => !m.IsGenericParameter))
            {
                member.TypeData = GetTypeData(member.MemberType, AutoAddType && member.MemberType.GetCustomAttribute<ReplicateAttribute>() != null);
            }
        }
    }
}
