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
        public MarshallMethod MarshallMethod;
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public readonly List<MemberInfo> ReplicatedMembers = new List<MemberInfo>();
        public readonly List<MethodInfo> RPCMethods = new List<MethodInfo>();
        public readonly bool IsInstanceRPC;
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
                MarshallMethod = MarshallMethod.Primitive;
            else
            {
                if (type.Implements(typeof(IEnumerable<>)))
                    MarshallMethod = MarshallMethod.Collection;
                else
                    MarshallMethod = MarshallMethod.Object;
            }
            TypeAttribute = type.GetCustomAttribute<ReplicateTypeAttribute>();
            IsInstanceRPC = TypeAttribute?.IsInstanceRPC ?? false;
            var surrogateType = TypeAttribute?.SurrogateType;
            if (surrogateType != null) SetSurrogate(surrogateType);

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            var autoMethods = TypeAttribute?.AutoMethods ?? AutoAdd.None;
            if (autoMethods != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            RPCMethods = type.GetMethods(bindingFlags)
                .Where(meth => meth.GetCustomAttribute<ReplicateIgnoreAttribute>() == null)
                .Where(meth => meth.DeclaringType == type)
                .Where(meth => autoMethods != AutoAdd.None || meth.GetCustomAttribute<ReplicateRPCAttribute>() != null)
                .ToList();
        }

        public void InitializeMembers()
        {
            var autoMembers = TypeAttribute?.AutoMembers ?? AutoAdd.None;
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (autoMembers != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            foreach (var field in Type.GetFields(bindingFlags))
            {
                if (Include(autoMembers, field))
                    AddMember(new MemberInfo(Model, field));
            }
            foreach (var property in Type.GetProperties(bindingFlags))
            {
                if (Include(autoMembers, property))
                    AddMember(new MemberInfo(Model, property));
            }
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
            MemberInfo member = null;
            if ((fieldInfo = Type.GetField(name)) != null)
                member = new MemberInfo(Model, fieldInfo);
            else if ((propInfo = Type.GetProperty(name)) != null)
                member = new MemberInfo(Model, propInfo);
            else
                throw new KeyNotFoundException(string.Format("Could not find member {0} in type {1}", name, Type.Name));
            AddMember(member);
            return this;
        }
        void AddMember(MemberInfo member)
        {
            ReplicatedMembers.Add(member);
            if (!member.MemberType.IsGenericParameter)
                Model.Add(member.MemberType);
        }
        public void SetSurrogate(Surrogate surrogate)
        {
            if (IsSurrogate)
                throw new InvalidOperationException("Cannot set the surrogate of a surrogate type");
            Model.Add(surrogate.Type).IsSurrogate = true;
            Surrogate = surrogate;
        }
    }
}
