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
        Type interfaceType;
        MethodInfo[] methods;
        public ReplicatedInterface(Type interfaceType)
        {
            this.interfaceType = interfaceType;
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
        public object Invoke(object target, ushort methodID, object[] args)
        {
            return methods[methodID].Invoke(target, args);
        }
    }
}
