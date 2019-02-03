using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.RPC
{
    public struct RPCContract
    {
        public readonly Type RequestType;
        public readonly Type ResponseType;
        public readonly MethodInfo Method;

        public RPCContract(Type requestType, Type responseType)
        {
            RequestType = requestType;
            ResponseType = responseType.GetTaskReturnType();
            Method = null;
        }
        public RPCContract(MethodInfo method)
        {
            var parameters = method.GetParameters();
            RequestType = parameters.Length == 1 ? parameters[0].ParameterType : typeof(None);
            ResponseType = method.ReturnType.GetTaskReturnType();
            Method = method;
        }
    }
}
