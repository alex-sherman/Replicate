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
        public string FullName { get; private set; }
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
            Name = type.Name;
            FullName = type.FullName;
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
            if (TypeAttribute == null) TypeAttribute = Type.GetCustomAttribute<ReplicateTypeAttribute>(false);
            IsInstanceRPC = TypeAttribute?.IsInstanceRPC ?? false;
            var surrogateType = TypeAttribute?.SurrogateType;
            if (surrogateType != null) SetSurrogate(surrogateType);

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            var autoMethods = TypeAttribute?.AutoMethods ?? AutoAdd.None;
            // TODO: Enforce unique names of methods
            Methods = Type.GetMethods(bindingFlags)
                .Where(meth => meth.DeclaringType.Namespace != "System")
                .Where(meth => !meth.IsSpecialName)
                .Where(meth => meth.GetCustomAttribute<ReplicateIgnoreAttribute>() == null)
                .Where(meth => meth.GetCustomAttribute<ReplicateRPCAttribute>() != null || autoMethods == AutoAdd.All || (autoMethods == AutoAdd.AllPublic && meth.IsPublic))
                .ToList();
            var autoMembers = TypeAttribute?.AutoMembers ?? AutoAdd.None;
            Members.Clear();
            foreach (var member in GetMembers(bindingFlags))
            {
                if (member.ParentType.Namespace == "System")
                    continue;
                if (member.GetAttribute<ReplicateIgnoreAttribute>() != null)
                    continue;
                if (member.GetAttribute<ReplicateAttribute>() != null || autoMembers == AutoAdd.All || (autoMembers == AutoAdd.AllPublic && member.IsPublic))
                    AddMember(member);
            }
        }

        IEnumerable<MemberInfo> GetMembers(BindingFlags bindingFlags)
        {
            foreach (var field in Type.GetFields(bindingFlags))
                if (!field.IsLiteral)
                    yield return new MemberInfo(Model, field);
            foreach (var property in Type.GetProperties(bindingFlags))
                yield return new MemberInfo(Model, property);
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
