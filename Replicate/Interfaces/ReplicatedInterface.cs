using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public class ReplicatedInterface
    {
        public Type InterfaceType;
        MethodInfo[] methods;
        public ReplicatedInterface(Type interfaceType)
        {
            // There appears to be no good way to convert a concrete Interface<string>.Method(string) -> Interface<T>.Method(T)
            // https://stackoverflow.com/questions/5218395/reflection-how-to-get-a-generic-method
            if (interfaceType.IsGenericType)
                throw new InvalidOperationException("Cannot use generic interface definitions as replicated interfaces");
            this.InterfaceType = interfaceType;
            methods = interfaceType.GetMethods();
        }
        public byte GetMethodID(MethodInfo info)
        {
            return (byte)Array.IndexOf(methods, info);
        }
        public MethodInfo GetMethodFromID(byte id)
        {
            return methods[id];
        }
        public object Invoke(object target, byte methodID, object[] args)
        {
            return GetMethodFromID(methodID).Invoke(target, args);
        }
    }
}
