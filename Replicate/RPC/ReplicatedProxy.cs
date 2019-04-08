using Replicate.Interfaces;
using Replicate.Messages;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Replicate.RPC
{
    public class ReplicatedProxy : IImplementor
    {
        ReplicateId? Target;
        IRPCChannel Channel;

        public ReplicatedProxy(ReplicateId? target, IRPCChannel channel, Type replicatedInterface)
        {
            Target = target;
            Channel = channel;
        }

        Task<object> RPC(MethodInfo method, object[] args)
        {
            return Channel.Request(method, new RPCRequest()
            {
                // TODO: This might be expensive, should maybe move to ReplicationModel
                Contract = new RPCContract(method),
                Target = Target,
                Request = args.Length > 0 ? args[0] : null,
            });
        }

        public T Intercept<T>(MethodInfo method, object[] args)
        {
            var result = RPC(method, args).GetAwaiter().GetResult();
            return (T)result;
        }

        public void InterceptVoid(MethodInfo method, object[] args)
        {
            RPC(method, args).GetAwaiter().GetResult();
        }

        public async Task<T> InterceptAsync<T>(MethodInfo method, object[] args)
        {
            return (T)(await RPC(method, args));
        }

        public Task InterceptAsyncVoid(MethodInfo method, object[] args)
        {
            return RPC(method, args);
        }
    }
}