using Replicate.Messages;
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
        public static ReplicationModel Default { get; } = new ReplicationModel();
        Dictionary<Type, TypeData> typeLookup = new Dictionary<Type, TypeData>();
        Dictionary<string, TypeData> stringLookup = new Dictionary<string, TypeData>();
        public TypeData this[Type type]
        {
            get { if (typeLookup.ContainsKey(type)) return typeLookup[type]; return null; }
        }
        public TypeData this[string typeName]
        {
            get { return stringLookup[typeName]; }
        }
        public TypeData Add(Type type)
        {
            var output = new TypeData(type);
            typeLookup.Add(type, output);
            stringLookup.Add(type.Name, output);
            return output;
        }
        public void LoadTypes(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes().Where(asm => asm.GetCustomAttribute<ReplicateAttribute>() != null))
            {
                Add(type);
            }
        }
        public void Compile()
        {
            foreach (var typeData in typeLookup.Values)
            {
                foreach (var member in typeData.ReplicatedMembers)
                {
                    member.TypeData = this[member.MemberType];
                }
            }
        }
    }
}
