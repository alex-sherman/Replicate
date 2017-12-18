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
        object target;
        public ReplicatedInterface(object target, Type interfaceType)
        {
            this.interfaceType = interfaceType;
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
        public object Invoke(ushort methodID, object[] args)
        {
            return methods[methodID].Invoke(target, args);
        }
    }
}
