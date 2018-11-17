using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Replicate.Interfaces
{
    public interface IImplementor
    {
        object Intercept(MethodInfo method, object[] args);
    }
    public class Implementor : IImplementor
    {
        public Func<MethodInfo, object[], object> Target;
        public Implementor(Func<MethodInfo, object[], object> target) => Target = target;
        public object Intercept(MethodInfo method, object[] args) => Target(method, args);
    }

    public class ProxyImplement
    {
        private const MethodAttributes ImplicitImplementation =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        static MethodInfo interceptInfo = typeof(ProxyImplement).GetMethod("Intercept", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo interceptInfoVoid = typeof(ProxyImplement).GetMethod("InterceptVoid", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo[] methods;
        public IImplementor Implementor { get; private set; }
        protected T Intercept<T>(int methodIndex, List<object> args)
        {
            object output = Implementor.Intercept(methods[methodIndex], args.ToArray());
            if (output == null)
                return default(T);
            return (T)output;
        }
        protected void InterceptVoid(int methodIndex, List<object> args)
        {
            Implementor.Intercept(methods[methodIndex], args.ToArray());
        }

        public static T HookUp<T>(IImplementor implementor)
        {
            Type target = typeof(T);
            AssemblyName assemblyName = new AssemblyName("DataBuilderAssembly");
            AssemblyBuilder assemBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemBuilder.DefineDynamicModule("DataBuilderModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(target.Name + "_Proxy", TypeAttributes.Class, typeof(ProxyImplement));
            typeBuilder.AddInterfaceImplementation(target);
            var methods = target.GetMethods();
            for (int i = 0; i < methods.Length; i++)
            {
                var info = methods[i];
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
            if (builder.ReturnType == typeof(void))
                il.Emit(OpCodes.Callvirt, interceptInfoVoid);
            else
                il.Emit(OpCodes.Callvirt, interceptInfo.MakeGenericMethod(info.ReturnType));
            il.Emit(OpCodes.Ret);
        }
    }
}
