using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public interface IImplementor
    {
        T Intercept<T>(MethodInfo method, object[] args);
        Task<T> InterceptAsync<T>(MethodInfo method, object[] args);
        Task InterceptAsyncVoid(MethodInfo method, object[] args);
        void InterceptVoid(MethodInfo method, object[] args);
    }

    public class ProxyImplement
    {
        private const MethodAttributes ImplicitImplementation =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        static MethodInfo interceptInfo = typeof(ProxyImplement).GetMethod("Intercept", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo interceptInfoAsync = typeof(ProxyImplement).GetMethod("InterceptAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo interceptInfoAsyncVoid = typeof(ProxyImplement).GetMethod("InterceptAsyncVoid", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo interceptInfoVoid = typeof(ProxyImplement).GetMethod("InterceptVoid", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo[] methods;
        public IImplementor Implementor { get; private set; }
        protected Task<T> InterceptAsync<T>(int methodIndex, List<object> args)
        {
            return Implementor.InterceptAsync<T>(methods[methodIndex], args.ToArray());
        }
        protected Task InterceptAsyncVoid(int methodIndex, List<object> args)
        {
            return Implementor.InterceptAsyncVoid(methods[methodIndex], args.ToArray());
        }
        protected T Intercept<T>(int methodIndex, List<object> args)
        {
            object output = Implementor.Intercept<T>(methods[methodIndex], args.ToArray());
            if (output == null)
                return default(T);
            return (T)output;
        }
        protected void InterceptVoid(int methodIndex, List<object> args)
        {
            Implementor.InterceptVoid(methods[methodIndex], args.ToArray());
        }

        public static T HookUp<T>(IImplementor implementor)
        {
            Type target = typeof(T);
            AssemblyName assemblyName = new AssemblyName("DataBuilderAssembly");
            AssemblyBuilder assemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
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
            else if (builder.ReturnType == typeof(Task))
                il.Emit(OpCodes.Callvirt, interceptInfoAsyncVoid);
            else if(builder.ReturnType.IsGenericType && builder.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                il.Emit(OpCodes.Callvirt, interceptInfoAsync.MakeGenericMethod(builder.ReturnType.GetGenericArguments()[0]));
            else
                il.Emit(OpCodes.Callvirt, interceptInfo.MakeGenericMethod(info.ReturnType));
            il.Emit(OpCodes.Ret);
        }
    }
}
