using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public class ReplicatedInterface<T>
    {
        Type interfaceType;
        MethodInfo[] methods;
        T target;
        public ReplicatedInterface(T target)
        {
            interfaceType = typeof(T);
            methods = interfaceType.GetMethods();
            this.target = target;
        }
        public byte GetMethodID(MethodInfo info)
        {
            return (byte)Array.IndexOf(methods, info);
        }
        public MethodInfo GetMethodFromID(byte id)
        {
            return methods[id];
        }
        public object Invoke(byte methodID, object[] args)
        {
            return methods[methodID].Invoke(target, args);
        }
    }
}
