using Replicate.Messages;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
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
        IRPCServer Server { get; set; }
    }

    public interface IRPCServer
    {
        /// <summary>
        /// Register a handler to respond to the given method
        /// </summary>
        void Respond(MethodInfo method, HandlerDelegate handler);
        Task<object> Handle(RPCRequest request);
        IEnumerable<MethodInfo> Methods { get; }
    }

    public class RPCServer : IRPCServer
    {
        struct HandlerInfo
        {
            public RPCContract Contract;
            public HandlerDelegate Handler;
        }
        Dictionary<MethodInfo, HandlerInfo> responders = new Dictionary<MethodInfo, HandlerInfo>();
        public IEnumerable<MethodInfo> Methods => responders.Keys;
        public virtual async Task<object> Handle(RPCRequest request)
        {
            if (responders.TryGetValue(request.Contract.Method, out var handlerInfo))
            {
                var task = handlerInfo.Handler(request);
                if (task != null)
                    return await task;
            }
            return None.Value;
        }
        public void Respond(MethodInfo method, HandlerDelegate handler)
        {
            responders[method] = new HandlerInfo()
            {
                Handler = handler,
                Contract = new RPCContract(method),
            };
        }
    }

    public abstract class RPCChannel<TEndpoint, TWireType> : IRPCChannel where TEndpoint : class
    {
        public IRPCServer Server { get; set; }
        public readonly IReplicateSerializer Serializer;
        Dictionary<TEndpoint, RPCContract> serverCache = new Dictionary<TEndpoint, RPCContract>();

        public RPCChannel(IReplicateSerializer serializer)
        {
            Serializer = serializer;
        }

        public abstract TEndpoint GetEndpoint(MethodInfo endpoint);
        public abstract Stream GetStream(TWireType wireValue);
        public abstract TWireType GetWireValue(Stream stream);
        public abstract Task<Stream> Request(TEndpoint messageId, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        public async Task<object> Request(MethodInfo method, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return Serializer.Deserialize(request.Contract.ResponseType, await Request(GetEndpoint(method), request, reliability));
        }
        public bool TryGetContract(TEndpoint endpoint, out RPCContract contract)
        {
            contract = default(RPCContract);
            if (!serverCache.ContainsKey(endpoint))
                serverCache = Server.Methods.ToDictionary(m => GetEndpoint(m), m => new RPCContract(m));
            if (serverCache.TryGetValue(endpoint, out contract))
            {
                return true;
            }
            return false;
        }

        public RPCRequest CreateRequest(TEndpoint endpoint, TWireType request, ReplicateId? target, out RPCContract contract)
        {
            if (!TryGetContract(endpoint, out contract))
                throw new ContractNotFoundError(endpoint.ToString());
            return new RPCRequest()
            {
                Contract = contract,
                Request = request == null ? null : Serializer.Deserialize(contract.RequestType, GetStream(request)),
                Target = target,
            };
        }

        public virtual async Task<TWireType> Receive(TEndpoint endpoint, TWireType request, ReplicateId? target = null)
        {
            var rpcRequest = CreateRequest(endpoint, request, target, out var contract);
            return GetWireValue(Serializer.Serialize(contract.ResponseType, (await Server.Handle(rpcRequest))));
        }
    }
}
