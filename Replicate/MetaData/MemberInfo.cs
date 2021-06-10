using Replicate.MetaData.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class MemberInfo
    {
        public const BindingFlags BindingAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public string Name { get { return Property?.Name ?? Field.Name; } }
        public ReplicationModel Model;
        public Type MemberType { get { return Property?.PropertyType ?? Field.FieldType; } }
        public Type DeclaringType { get => Property?.DeclaringType ?? Field.DeclaringType; }
        public bool IsStatic { get => Field?.IsStatic ?? (Property?.GetGetMethod() ?? Property.GetSetMethod())?.IsStatic ?? false; }
        public bool IsPublic { get => Field?.IsPublic ?? (Property?.GetGetMethod() ?? Property.GetSetMethod())?.IsPublic ?? false; }
        public Surrogate Surrogate { get; private set; }
        public bool IsGenericParameter { get { return MemberType.IsGenericParameter; } }
        public readonly FieldInfo Field;
        public readonly PropertyInfo Property;
        public int GenericParameterPosition { get { return MemberType.GenericParameterPosition; } }
        public T GetAttribute<T>() where T : Attribute
        {
            return Field != null ? Field.GetCustomAttribute<T>() : Property.GetCustomAttribute<T>();
        }
        public MemberInfo(ReplicationModel model, FieldInfo field)
        {
            Field = field;
            Initialize(model);
        }
        public MemberInfo(ReplicationModel model, PropertyInfo property)
        {
            Property = property;
            Initialize(model);
        }
        private void Initialize(ReplicationModel model)
        {
            Model = model;
            if (GetAttribute<AsReferenceAttribute>() != null)
                SetSurrogate(typeof(ReplicatedReference<>).MakeGenericType(MemberType));
        }
        public Type GetMemberType(Type declaringType)
        {
            if (IsGenericParameter)
                return declaringType.GetGenericArguments()[GenericParameterPosition];
            else
                return MemberType;
        }
        public FieldInfo GetField(Type declaringType)
        {
            return declaringType.GetField(Name, BindingAll);
        }
        public PropertyInfo GetProperty(Type declaringType)
        {
            return declaringType.GetProperty(Name, BindingAll);
        }
        public void SetSurrogate(Surrogate surrogate)
        {
            Surrogate = surrogate;
        }
        public override string ToString()
        {
            return $"{DeclaringType}.{Name}";
        }
    }
}
