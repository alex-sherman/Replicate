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
        private FieldInfo field;
        private PropertyInfo property;
        private Func<object, object> getter;
        public MemberInfo(FieldInfo field, byte id)
        {
            ID = id;
            this.field = field;

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
        public MemberInfo(PropertyInfo property, byte id)
        {
            ID = id;
            this.property = property;
            getter = property.GetValue;
        }
        public Type MemberType { get { return property?.PropertyType ?? field?.FieldType; } }
        public TypeData TypeData { get; set; }

        public void SetValue(object obj, object value) { property?.SetValue(obj, value); field?.SetValue(obj, value); }
        public object GetValue(object obj)
        {
            return getter(obj);
        }
    }
}
