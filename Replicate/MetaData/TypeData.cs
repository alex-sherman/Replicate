using Replicate.MetaTyping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class TypeData
    {
        public MarshalMethod MarshalMethod;
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public readonly List<MemberInfo> ReplicatedMembers = new List<MemberInfo>();
        public readonly List<MethodInfo> RPCMethods = new List<MethodInfo>();
        public readonly bool IsInstanceRPC;
        public List<Type> ReplicatedInterfaces;
        public Surrogate Surrogate { get; private set; }
        public ReplicationModel Model { get; private set; }
        private bool IsSurrogate = false;
        public readonly ReplicateTypeAttribute TypeAttribute;
        public TypeData(Type type, ReplicationModel model)
        {
            Type = type;
            Name = type.FullName;
            Model = model;
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                MarshalMethod = MarshalMethod.Primitive;
            else
            {
                if (type == typeof(ICollection<>) || type.GetInterface("ICollection`1") != null || type == typeof(IEnumerable<>))
                    MarshalMethod = MarshalMethod.Collection;
                else
                    MarshalMethod = MarshalMethod.Object;
            }
            TypeAttribute = type.GetCustomAttribute<ReplicateTypeAttribute>();
            IsInstanceRPC = TypeAttribute?.IsInstanceRPC ?? false;
            var surrogateType = TypeAttribute?.SurrogateType;
            if (surrogateType != null) SetSurrogate(surrogateType);
            var autoMembers = TypeAttribute?.AutoMembers ?? AutoAdd.None;
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (autoMembers != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (Include(autoMembers, field))
                    AddMember(field);
            }
            foreach (var property in type.GetProperties(bindingFlags))
            {
                if (Include(autoMembers, property))
                    AddMember(property);
            }

            bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            var autoMethods = TypeAttribute?.AutoMethods ?? AutoAdd.None;
            if (autoMethods != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            RPCMethods = type.GetMethods(bindingFlags)
                .Where(meth => meth.GetCustomAttribute<ReplicateIgnoreAttribute>() == null)
                .Where(meth => meth.DeclaringType == type)
                .Where(meth => autoMethods != AutoAdd.None || meth.GetCustomAttribute<ReplicateRPCAttribute>() != null)
                .ToList();
            ReplicatedInterfaces = type.GetInterfaces()
                .Where(interfaceType => interfaceType.GetCustomAttribute<ReplicateAttribute>() != null)
                .ToList();
        }

        public bool Include(AutoAdd autoMembers, System.Reflection.MemberInfo member)
        {
            if (member.GetCustomAttribute<ReplicateIgnoreAttribute>() != null)
                return false;
            return autoMembers != AutoAdd.None || member.GetCustomAttribute<ReplicateAttribute>() != null;
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
            ReplicatedMembers.Add(new MemberInfo(Model, field, (byte)ReplicatedMembers.Count));
        }
        private void AddMember(PropertyInfo property)
        {
            ReplicatedMembers.Add(new MemberInfo(Model, property, (byte)ReplicatedMembers.Count));
        }
        public void SetSurrogate(Surrogate surrogate)
        {
            if (IsSurrogate)
                throw new InvalidOperationException("Cannot set the surrogate of a surrogate type");
            Model[surrogate.Type].IsSurrogate = true;
            Surrogate = surrogate;
        }
    }
}
