using Replicate.Messages;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public class ReplicatedProxy : IImplementor
    {
        ReplicatedID? Target;
        ReplicationManager Manager;
        Type Interface;

        public ReplicatedProxy(ReplicatedID? target, ReplicationManager manager, Type replicatedInterface)
        {
            Target = target;
            Manager = manager;
            Interface = replicatedInterface;
        }

        Task<object> RPC(MethodInfo method, object[] args)
        {
            var parms = method.GetParameters();
            bool hasParam = parms.Length == 1;
            return Manager.Publish(new RPCRequest()
            {
                Method = method,
                Request = hasParam ? args[0] : None.Value,
                RequestType = hasParam ? parms[0].ParameterType : typeof(None),
                ResponseType = method.ReturnType,
                Target = Target,
            });
        }

        public T Intercept<T>(MethodInfo method, object[] args)
        {
            return (T)RPC(method, args).Result;
        }

        public void InterceptVoid(MethodInfo method, object[] args)
        {
            RPC(method, args);
        }

        public async Task<T> InterceptAsync<T>(MethodInfo method, object[] args)
        {
            var result = (await RPC(method, args));
            return (T)result;
        }

        public async Task InterceptAsyncVoid(MethodInfo method, object[] args)
        {
            await RPC(method, args);
        }
    }
}