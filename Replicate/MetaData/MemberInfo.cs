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
        public int GenericParameterPosition { get { return MemberType.GenericParameterPosition; } }
        private FieldInfo field;
        private PropertyInfo property;
        private Func<object, object> getter;
        public MemberInfo(FieldInfo field, byte id, int? genericParameterPosition = null)
        {
            ID = id;
            this.field = field;
            if (IsGenericParameter)
            {
                getter = new Func<object, object>((obj) => obj.GetType().GetField(field.Name).GetValue(obj));
            }
            else
            {
                var meth = new DynamicMethod("getter", typeof(object), new Type[] { typeof(object) });
                var il = meth.GetILGenerator();
                il.Emit(OpCodes.Ldarg, 0);
                if (field.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, field.DeclaringType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, field.DeclaringType);
                }
                il.Emit(OpCodes.Ldfld, field);
                if (MemberType.IsValueType)
                    il.Emit(OpCodes.Box, MemberType);
                il.Emit(OpCodes.Ret);
                getter = (Func<object, object>)meth.CreateDelegate(
                    typeof(Func<object, object>));
            }
        }
        public MemberInfo(PropertyInfo property, byte id, int? genericParameterPosition = null)
        {
            ID = id;
            this.property = property;
            if (IsGenericParameter)
            {
                getter = new Func<object, object>((obj) => obj.GetType().GetProperty(property.Name).GetValue(obj));
            }
            else
            {
                getter = property.GetValue;
            }
        }

        public void SetValue(object obj, object value)
        {
            if (IsGenericParameter)
            {
                if (property != null)
                    obj.GetType().GetProperty(property.Name).SetValue(obj, value);
                if (field != null)
                    obj.GetType().GetField(field.Name).SetValue(obj, value);
            }
            else
            {
                property?.SetValue(obj, value);
                field?.SetValue(obj, value);
            }
        }
        public object GetValue(object obj)
        {
            return getter(obj);
        }
    }
}
