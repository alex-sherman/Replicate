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

        public ReplicatedProxy(ReplicatedID? target, ReplicationManager manager, Type replicatedInterface)
        {
            Target = target;
            Manager = manager;
        }

        Task<object> RPC(MethodInfo method, object[] args)
        {
            return Manager.Channel.Publish(method, new RPCRequest()
            {
                Contract = new RPCContract(method),
                Target = Target,
                Request = args.Length == 1 ? args[0] : null,
            });
        }

        public T Intercept<T>(MethodInfo method, object[] args)
        {
            return (T)RPC(method, args).GetAwaiter().GetResult();
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