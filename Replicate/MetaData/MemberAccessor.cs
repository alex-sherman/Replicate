using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class MemberAccessor
    {
        private Func<object, object> getter;
        private MemberInfo info;
        public TypeAccessor TypeAccessor { get; private set; }
        public Type Type { get; private set; }
        public Type DeclaringType { get; private set; }

        public MemberAccessor(MemberInfo info, TypeAccessor declaringType, ReplicationModel model)
        {
            DeclaringType = declaringType.Type;
            Type = info.GetMemberType(declaringType.Type);
            TypeAccessor = info.TypeData?.GetAccessor(Type) ?? model[Type]?.GetAccessor(Type);
            this.info = info;
            if (info.field != null)
            {
                var meth = new DynamicMethod("getter", typeof(object), new Type[] { typeof(object) });
                var il = meth.GetILGenerator();
                il.Emit(OpCodes.Ldarg, 0);
                if (DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, DeclaringType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, DeclaringType);
                }
                il.Emit(OpCodes.Ldfld, info.GetField(DeclaringType));
                if (Type.IsValueType)
                    il.Emit(OpCodes.Box, Type);
                il.Emit(OpCodes.Ret);
                getter = (Func<object, object>)meth.CreateDelegate(
                    typeof(Func<object, object>));
            }
            else
            {
                getter = info.GetProperty(DeclaringType).GetValue;
            }
        }

        public void SetValue(object obj, object value)
        {
            if (info.IsGenericParameter)
            {
                if (info.property != null)
                    obj.GetType().GetProperty(info.property.Name).SetValue(obj, value);
                if (info.field != null)
                    obj.GetType().GetField(info.field.Name).SetValue(obj, value);
            }
            else
            {
                info.property?.SetValue(obj, value);
                info.field?.SetValue(obj, value);
            }
        }
        public object GetValue(object obj)
        {
            return getter(obj);
        }
    }
}
