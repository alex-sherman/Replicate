using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaTyping
{
    public interface IInterceptor
    {
        T Intercept<T>(MethodInfo method, object[] args);
    }

    public class ProxyImplement
    {
        private class Void { };
        private const MethodAttributes ImplicitImplementation =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        static MethodInfo interceptInfo = typeof(ProxyImplement).GetMethod("Intercept", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo interceptInfoVoid = typeof(ProxyImplement).GetMethod("InterceptVoid", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo[] methods;
        public IInterceptor Implementor { get; private set; }
        protected T Intercept<T>(int methodIndex, List<object> args)
        {
            object output = Implementor.Intercept<T>(methods[methodIndex], args.ToArray());
            if (output == null)
                return default(T);
            return (T)output;
        }
        protected void InterceptVoid(int methodIndex, List<object> args)
        {
            Implementor.Intercept<Void>(methods[methodIndex], args.ToArray());
        }

        public static T HookUp<T>(IInterceptor implementor)
        {
            Type target = typeof(T);
            TypeBuilder typeBuilder = DynamicModule.Create().DefineType(target.Name + "_Proxy", TypeAttributes.Class, typeof(ProxyImplement));
            typeBuilder.AddInterfaceImplementation(target);
            var methods = target.GetMethods();
            for (int i = 0; i < methods.Length; i++)
            {
                var info = methods[i];
                if (info.ContainsGenericParameters)
                    throw new ReplicateError("Generic parameters in RPC interfaces are not supported");
                var builder = typeBuilder.DefineMethod(info.Name, ImplicitImplementation,
                    info.ReturnType, info.GetParameters().Select(pinfo => pinfo.ParameterType).ToArray());
                Implement(i, builder, info);
            }

            Type type = typeBuilder.CreateType();
            var output = (T)type.GetConstructor(new Type[] { }).Invoke(new object[] { });
            (output as ProxyImplement).Implementor = implementor;
            (output as ProxyImplement).methods = methods;
            return output;
        }

        static void Implement(int methodIndex, MethodBuilder builder, MethodInfo info)
        {
            var il = builder.GetILGenerator();
            LocalBuilder arr = il.DeclareLocal(typeof(List<object>), true);
            il.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);
            var paramInfos = info.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg, i + 1);
                if (paramInfo.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, paramInfo.ParameterType);
                il.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add"));
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, methodIndex);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, builder.ReturnType == typeof(void)
                ? interceptInfoVoid : interceptInfo.MakeGenericMethod(info.ReturnType));
            il.Emit(OpCodes.Ret);
        }
    }
}
