using Replicate.MetaTyping;
using Replicate.Serialization;
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
        public PrimitiveType PrimitiveType;
        public readonly bool PrefixWithType;
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public IEnumerable<RepKey> Keys => Members.Keys;
        public MemberInfo this[RepKey key] => Members[key];
        public readonly RepSet<MemberInfo> Members = new RepSet<MemberInfo>();
        public readonly string[] GenericTypeParameters = null;
        public List<MethodInfo> Methods = new List<MethodInfo>();
        public bool IsInstanceRPC;
        public Surrogate Surrogate { get; private set; }
        public ReplicationModel Model { get; private set; }
        private bool IsSurrogate = false;
        public ReplicateTypeAttribute TypeAttribute;
        public TypeData(Type type, ReplicationModel model)
        {
            Type = type;
            Name = type.FullName;
            Model = model;
            PrefixWithType = type == typeof(object);
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                MarshallMethod = MarshallMethod.Primitive;
                PrimitiveType = PrimitiveTypeMap.MapType(type);
            }
            else if (typeof(Blob).IsAssignableFrom(type))
            {
                MarshallMethod = MarshallMethod.Blob;
            }
            else
            {
                if (type.Implements(typeof(IEnumerable<>)))
                    MarshallMethod = MarshallMethod.Collection;
                else
                    MarshallMethod = MarshallMethod.Object;
            }
            if (type.IsGenericTypeDefinition)
                GenericTypeParameters = type.GetTypeInfo().GenericTypeParameters.Select(v => v.Name).ToArray();
        }

        public void InitializeMembers()
        {
            if (TypeAttribute == null) TypeAttribute = Type.GetCustomAttribute<ReplicateTypeAttribute>();
            IsInstanceRPC = TypeAttribute?.IsInstanceRPC ?? false;
            var surrogateType = TypeAttribute?.SurrogateType;
            if (surrogateType != null) SetSurrogate(surrogateType);

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
            var autoMethods = TypeAttribute?.AutoMethods ?? AutoAdd.None;
            if (autoMethods != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            // TODO: Enforce unique names of methods
            Methods = Type.GetMethods(bindingFlags)
                .Where(meth => meth.DeclaringType.Namespace != "System")
                .Where(meth => !meth.IsSpecialName)
                .Where(meth => meth.GetCustomAttribute<ReplicateIgnoreAttribute>() == null)
                .Where(meth => autoMethods != AutoAdd.None || meth.GetCustomAttribute<ReplicateRPCAttribute>() != null)
                .ToList();
            var autoMembers = TypeAttribute?.AutoMembers ?? AutoAdd.None;
            bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (autoMembers != AutoAdd.AllPublic)
                bindingFlags |= BindingFlags.NonPublic;
            Members.Clear();
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
            if (member.DeclaringType.Namespace == "System") return false;
            if (member.GetCustomAttribute<ReplicateIgnoreAttribute>() != null)
                return false;
            return autoMembers != AutoAdd.None || member.GetCustomAttribute<ReplicateAttribute>() != null;
        }

        public TypeData AddMember(string name)
        {
            FieldInfo fieldInfo;
            PropertyInfo propInfo;
            MemberInfo member;
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
            Members.Add(member.Name, member);
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
        public override string ToString()
        {
            return $"TypeData: {Name}";
        }
    }
}
