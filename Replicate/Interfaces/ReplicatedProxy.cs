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

        Task<TypedValue> RPC(MethodInfo method, object[] args)
        {
            var message = new RPCMessage()
            {
                ReplicatedID = Target,
                InterfaceType = Interface,
                Method = method,
                Args = args.Select(arg => new TypedValue(arg)).ToList(),
            };
            return Manager.Channel.Publish<RPCMessage, TypedValue>(message);
        }

        public T Intercept<T>(MethodInfo method, object[] args)
        {
            return (T)RPC(method, args).Result.Value;
        }

        public void InterceptVoid(MethodInfo method, object[] args)
        {
            RPC(method, args);
        }

        public async Task<T> InterceptAsync<T>(MethodInfo method, object[] args)
        {
            var result = (await RPC(method, args)).Value;
            return (T)result;
        }

        public async Task InterceptAsyncVoid(MethodInfo method, object[] args)
        {
            await RPC(method, args);
        }
    }
}