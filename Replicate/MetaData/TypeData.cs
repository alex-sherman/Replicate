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
    public class TypeData
    {
        public string Name { get; private set; }
        public List<MemberInfo> ReplicatedMembers = new List<MemberInfo>();
        public Type Type { get; private set; }
        public ReplicationPolicy Policy;
        public TypeData(Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
                Policy.MarshalMethod = MarshalMethod.Primitive;
            else if (type.IsValueType)
                Policy.MarshalMethod = MarshalMethod.Value;
            else
                Policy.MarshalMethod = MarshalMethod.Reference;

            ReplicateAttribute replicateAttribute = type.GetCustomAttribute<ReplicateAttribute>();
            if (replicateAttribute != null && replicateAttribute.MarshalMethod.HasValue)
                Policy.MarshalMethod = replicateAttribute.MarshalMethod.Value;
            Type = type;
            Name = type.FullName;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    AddMember(field);
            }
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    AddMember(property);
            }
        }
        public TypeData AddMember(string name)
        {
            FieldInfo fieldInfo;
            PropertyInfo propInfo;
            if ((fieldInfo = Type.GetField(name)) != null)
                AddMember(fieldInfo);
            else if ((propInfo = Type.GetProperty(name)) != null)
                AddMember(propInfo);
            else
                throw new KeyNotFoundException(string.Format("Could not find member {0} in type {1}", name, Type.Name));
            return this;
        }

        private void AddMember(FieldInfo field)
        {
            ReplicatedMembers.Add(new MemberInfo(field, (byte)ReplicatedMembers.Count));
        }
        private void AddMember(PropertyInfo property)
        {
            ReplicatedMembers.Add(new MemberInfo(property, (byte)ReplicatedMembers.Count));
        }
        public object Construct()
        {
            var cinfo = Type.GetConstructor(new Type[] { });
            return cinfo.Invoke(new object[] { });
        }
    }
}
