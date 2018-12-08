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
        IReplicationChannel Channel;

        public ReplicatedProxy(ReplicatedID? target, IReplicationChannel channel, Type replicatedInterface)
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
            return (T)RPC(method, args).GetAwaiter().GetResult();
        }

        public void InterceptVoid(MethodInfo method, object[] args)
        {
            RPC(method, args);
        }

        public async Task<T> InterceptAsync<T>(MethodInfo method, object[] args)
        {
            var result = (await RPC(method, args));
            try
            {
                return (T)result;
            }
            catch(InvalidCastException)
            {
                throw;
            }
        }

        public async Task InterceptAsyncVoid(MethodInfo method, object[] args)
        {
            await RPC(method, args);
        }
    }
}