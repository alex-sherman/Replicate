using Replicate.MetaData.Policy;
using System;
using System.Reflection.Emit;

namespace Replicate.MetaData {
    public class MemberAccessor {
        private Func<object, object> getter;
        private Action<object, object> setter;
        public MemberInfo Info { get; private set; }
        public readonly bool SkipNull;
        public readonly bool SkipEmpty;
        public readonly bool IsNullable;
        public bool CanRead => getter != null;
        public bool CanWrite => setter != null;
        public TypeAccessor TypeAccessor { get; private set; }
        public SurrogateAccessor Surrogate { get; private set; }
        public Type Type { get; private set; }
        public Type DeclaringType { get; private set; }
        private static readonly bool AllowEmit = false;
        static MemberAccessor() {
            try {
                new DynamicMethod("getter", typeof(object), new Type[] { typeof(object) });
            } catch { AllowEmit = false; }
        }

        public MemberAccessor(MemberInfo info, TypeAccessor declaringType, ReplicationModel model) {
            if (info.IsStatic) throw new InvalidOperationException("Can't access static members");
            DeclaringType = declaringType.Type;
            Type = info.GetMemberType(declaringType.Type);
            TypeAccessor = model.GetTypeAccessor(Type);
            if (info.Surrogate != null)
                Surrogate = new SurrogateAccessor(TypeAccessor, info.Surrogate, model);
            Info = info;
            SkipNull = info.GetAttribute<SkipNullAttribute>() != null;
            SkipEmpty = info.GetAttribute<SkipEmptyAttribute>() != null;
            if (SkipEmpty && TypeAccessor.TypeData.MarshallMethod != MarshallMethod.Collection)
                throw new InvalidOperationException("Can't SkipEmpty on a non-collection type");
            IsNullable = info.GetAttribute<NullableAttribute>() != null
                || (TypeAccessor.IsNullable && info.GetAttribute<NonNullAttribute>() == null);
            if (info.Field != null) {
                var field = info.GetField(DeclaringType);
                if (AllowEmit) {
                    var meth = new DynamicMethod("getter", typeof(object), new Type[] { typeof(object) });
                    var il = meth.GetILGenerator();
                    il.Emit(OpCodes.Ldarg, 0);
                    if (DeclaringType.IsValueType)
                        il.Emit(OpCodes.Unbox, DeclaringType);
                    else
                        il.Emit(OpCodes.Castclass, DeclaringType);
                    il.Emit(OpCodes.Ldfld, field);
                    if (Type.IsValueType)
                        il.Emit(OpCodes.Box, Type);
                    il.Emit(OpCodes.Ret);
                    getter = (Func<object, object>)meth.CreateDelegate(
                        typeof(Func<object, object>));
                    setter = field.SetValue;
                } else {
                    getter = field.GetValue;
                    setter = field.SetValue;
                }
            } else {
                var prop = info.GetProperty(DeclaringType);
                if (prop.CanRead)
                    getter = prop.GetValue;
                if (prop.CanWrite)
                    setter = prop.SetValue;
            }
        }

        public void SetValue(object obj, object value) {
            setter?.Invoke(obj, value);
        }
        public object GetValue(object obj) {
            return getter(obj);
        }
        public override string ToString() => $"{Info}";
    }
}
