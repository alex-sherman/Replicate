using Replicate.MetaData.Policy;
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
        private Action<object, object> setter;
        public MemberInfo Info { get; private set; }
        public readonly bool SkipNull;
        public TypeAccessor TypeAccessor { get; private set; }
        public TypeAccessor Surrogate { get; private set; }
        public Type Type { get; private set; }
        public Type DeclaringType { get; private set; }

        public MemberAccessor(MemberInfo info, TypeAccessor declaringType, ReplicationModel model)
        {
            Surrogate = info.Surrogate;
            DeclaringType = declaringType.Type;
            Type = info.GetMemberType(declaringType.Type);
            TypeAccessor = model.GetTypeAccessor(Type);
            Info = info;
            SkipNull = info.GetAttribute<SkipNullAttribute>() != null;
            if (info.Field != null)
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
                setter = info.GetField(DeclaringType).SetValue;
            }
            else
            {
                getter = info.GetProperty(DeclaringType).GetValue;
                setter = info.GetProperty(DeclaringType).SetValue;
            }
        }

        public void SetValue(object obj, object value)
        {
            try
            {
                setter(obj, value);
            }
            catch { }
        }
        public object GetValue(object obj)
        {
            try
            {
                return getter(obj);
            }
            catch
            {
                return null;
            }
        }
        public override string ToString()
        {
            return Info.ToString();
        }
    }
}
