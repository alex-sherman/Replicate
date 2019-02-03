using Replicate.Messages;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.RPC
{
    [Flags]
    public enum ReliabilityMode
    {
        ReliableSequenced = Reliable | Sequenced,
        Reliable = 0b0001,
        Sequenced = 0b0010,
    }
    public delegate Task<object> HandlerDelegate(RPCRequest request);
    public interface IRPCChannel
    {
        Task<object> Request(MethodInfo method, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        void Respond(MethodInfo method, HandlerDelegate handler);
    }

    public abstract class RPCChannel<TEndpoint, TWireType> : IRPCChannel where TEndpoint : class
    {
        struct HandlerInfo
        {
            public RPCContract Contract;
            public HandlerDelegate Handler;
        }
        public abstract IReplicateSerializer<TWireType> Serializer { get; }
        Dictionary<TEndpoint, HandlerInfo> responders = new Dictionary<TEndpoint, HandlerInfo>();
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        public bool IsOpen { get; protected set; }

        public abstract TEndpoint GetEndpoint(MethodInfo endpoint);

        public abstract Task<TWireType> Request(TEndpoint messageId, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        public async Task<object> Request(MethodInfo method, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return Serializer.Deserialize(request.Contract.ResponseType, await Request(GetEndpoint(method), request, reliability));
        }
        public void Respond(MethodInfo method, HandlerDelegate handler)
        {
            responders[GetEndpoint(method)] = new HandlerInfo()
            {
                Handler = handler,
                Contract = new RPCContract(method),
            };
        }
        public bool TryGetContract(TEndpoint endpoint, out RPCContract contract)
        {
            contract = default(RPCContract);
            if (responders.TryGetValue(endpoint, out var handler))
            {
                contract = handler.Contract;
                return true;
            }
            return false;
        }

        protected virtual async Task<object> Receive(TEndpoint endpoint, RPCRequest request)
        {
            if (responders.TryGetValue(endpoint, out var handlerInfo))
            {
                var task = handlerInfo.Handler(request);
                if (task != null)
                    return await task;
            }
            return None.Value;
        }
        public virtual async Task<TWireType> Receive(TEndpoint endpoint, TWireType request, ReplicateId? target = null)
        {
            if (!TryGetContract(endpoint, out var contract))
                throw new ContractNotFoundError(endpoint.ToString());
            var rpcRequest = new RPCRequest()
            {
                Contract = contract,
                Request = request == null ? null : Serializer.Deserialize(contract.RequestType, request),
                Target = target,
            };
            return Serializer.Serialize(contract.ResponseType, (await Receive(endpoint, rpcRequest)));
        }
    }
}
