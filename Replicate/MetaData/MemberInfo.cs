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
        public string Name { get { return property?.Name ?? field?.Name; } }
        public Type MemberType { get { return property?.PropertyType ?? field?.FieldType; } }
        public TypeData TypeData { get; set; }
        public bool IsGenericParameter { get { return MemberType.IsGenericParameter; } }
        internal FieldInfo field;
        internal PropertyInfo property;
        public int GenericParameterPosition { get { return MemberType.GenericParameterPosition; } }
        public T GetAttribute<T>() where T : Attribute
        {
            return field != null ? field.GetCustomAttribute<T>() : property.GetCustomAttribute<T>();
        }
        public MemberInfo(FieldInfo field, byte id)
        {
            ID = id;
            this.field = field;
        }
        public MemberInfo(PropertyInfo property, byte id)
        {
            ID = id;
            this.property = property;
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
    }
}
