using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class TypeData
    {
        public string Name { get; private set; }
        public List<MemberInfo> ReplicatedMembers = new List<MemberInfo>();
        public Type Type { get; private set; }
        public TypeData(Type type)
        {
            Type = type;
            Name = type.Name;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    ReplicatedMembers.Add(new MemberInfo(field, (byte)ReplicatedMembers.Count));
            }
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    ReplicatedMembers.Add(new MemberInfo(property, (byte)ReplicatedMembers.Count));
            }
        }
        public object Construct()
        {
            var cinfo = Type.GetConstructor(new Type[] { });
            return cinfo.Invoke(new object[] { });
        }
        public byte[] GetBytes(object replicate)
        {
            MemoryStream stream = new MemoryStream();
            // Only replicate primitives
            /// TODO: Replicate members by reference or copy depending on <see cref="ReplicationPolicy"/>
            foreach (var member in ReplicatedMembers.Where(m => m.TypeData == null))
            {
                Serializer.NonGeneric.Serialize(stream, member.GetValue(replicate));
            }
            return stream.ToArray();
        }
    }
}
