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
        public byte ID { get; private set; }
        public string Name { get { return Property?.Name ?? Field.Name; } }
        public ReplicationModel Model;
        public Type MemberType { get { return Property?.PropertyType ?? Field.FieldType; } }
        public Type ParentType { get => Property?.DeclaringType ?? Field.DeclaringType; }
        public TypeData TypeData { get; set; }
        public TypeAccessor Surrogate { get; private set; }
        public bool IsGenericParameter { get { return MemberType.IsGenericParameter; } }
        internal FieldInfo Field;
        internal PropertyInfo Property;
        public int GenericParameterPosition { get { return MemberType.GenericParameterPosition; } }
        public T GetAttribute<T>() where T : Attribute
        {
            return Field != null ? Field.GetCustomAttribute<T>() : Property.GetCustomAttribute<T>();
        }
        public MemberInfo(ReplicationModel model, FieldInfo field, byte id)
        {
            Field = field;
            Initialize(model, id);
        }
        public MemberInfo(ReplicationModel model, PropertyInfo property, byte id)
        {
            Property = property;
            Initialize(model, id);
        }
        private void Initialize(ReplicationModel model, byte id)
        {
            Model = model;
            ID = id;
            if (GetAttribute<ReplicatePolicyAttribute>()?.AsReference ?? false)
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
            return declaringType.GetField(Name);
        }
        public PropertyInfo GetProperty(Type declaringType)
        {
            return declaringType.GetProperty(Name);
        }
        public void SetSurrogate(Type surrogate)
        {
            if (surrogate.IsGenericTypeDefinition)
                throw new InvalidOperationException("Cannot set a surrogate type that is generic");
            Surrogate = Model.GetTypeAccessor(surrogate);
        }
        public override string ToString()
        {
            return $"{ParentType}.{Name}";
        }
    }
}
