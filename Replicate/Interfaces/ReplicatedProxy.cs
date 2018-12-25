using Replicate.Messages;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public class ReplicatedProxy : IImplementor
    {
        ReplicatedId? Target;
        IReplicationChannel Channel;

        public ReplicatedProxy(ReplicatedId? target, IReplicationChannel channel, Type replicatedInterface)
        {
            Target = target;
            Channel = channel;
        }

        Task<object> RPC(MethodInfo method, object[] args)
        {
            return Channel.Request(method, new RPCRequest()
            {
                Contract = new RPCContract(method),
                Target = Target,
                Request = args.Length == 1 ? args[0] : null,
            });
        }

        public T Intercept<T>(MethodInfo method, object[] args)
        {
            var result = RPC(method, args).GetAwaiter().GetResult();
            return (T)result;
        }

        public void InterceptVoid(MethodInfo method, object[] args)
        {
            RPC(method, args);
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