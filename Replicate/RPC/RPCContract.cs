using System;
using System.Linq;
using System.Reflection;

namespace Replicate.RPC {
    public struct RPCContract {
        public readonly Type RequestType;
        public readonly Type ResponseType;
        public RPCContract(Type requestType, Type responseType) {
            RequestType = requestType;
            ResponseType = responseType;
        }
        public RPCContract(MethodInfo method) {
            var parameters = method.GetParameters().Where(p => !p.IsOptional);
            if (parameters.Skip(1).Any())
                throw new ReplicateError($"Invalid contract {method} with multiple required parameters");
            RequestType = parameters.FirstOrDefault()?.ParameterType ?? typeof(None);
            ResponseType = method.ReturnType.GetTaskReturnType();
        }
    }
}
