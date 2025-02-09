using Replicate.Messages;
using Replicate.MetaTyping;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Replicate.RPC {
    public class ReplicatedProxy : IInterceptor {
        ReplicateId? Target;
        IRPCChannel Channel;

        public ReplicatedProxy(ReplicateId? target, IRPCChannel channel, Type replicatedInterface) {
            Target = target;
            Channel = channel;
        }

        Task<object> RPC(MethodInfo method, object[] args) {
            return Channel.Request(new RPCRequest() {
                // TODO: This might be expensive, should maybe move to ReplicationModel
                Endpoint = Channel.Model.MethodKey(method),
                Contract = new RPCContract(method),
                Target = Target,
                Request = args.Length > 0 ? args[0] : null,
            });
        }

        public T Intercept<T>(MethodInfo method, object[] args) {
            return TypeUtil.CastObjectTask<T>(RPC(method, args));
        }
    }
}