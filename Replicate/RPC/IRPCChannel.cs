using Replicate.Messages;
using Replicate.MetaData;
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
        Task<object> Request(RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        IRPCServer Server { get; }
        ReplicationModel Model { get; }
    }

    public interface IRPCServer
    {
        ReplicationModel Model { get; }
        /// <summary>
        /// Register a handler to respond to the given method
        /// </summary>
        void Respond(MethodKey key, HandlerDelegate handler);
        Task<object> Handle(RPCRequest request);
        IEnumerable<MethodKey> Methods { get; }
        RPCContract Contract(MethodKey method);
    }

    public class RPCServer : IRPCServer
    {
        struct HandlerInfo
        {
            public RPCContract Contract;
            public HandlerDelegate Handler;
        }
        Dictionary<MethodKey, HandlerInfo> responders = new Dictionary<MethodKey, HandlerInfo>();
        public IEnumerable<MethodKey> Methods => responders.Keys;
        public RPCContract Contract(MethodKey method) => responders[method].Contract;
        public ReplicationModel Model { get; }
        public RPCServer(ReplicationModel model) { Model = model; }
        public virtual async Task<object> Handle(RPCRequest request)
        {
            if (responders.TryGetValue(request.Endpoint, out var handlerInfo))
            {
                var task = handlerInfo.Handler(request);
                if (task != null)
                    return await task;
            }
            throw new ContractNotFoundError();
        }
        public void Respond(MethodKey method, HandlerDelegate handler)
        {
            responders[method] = new HandlerInfo()
            {
                Handler = handler,
                Contract = new RPCContract(Model.GetMethod(method)),
            };
        }
    }

    public abstract class RPCChannel<TWireType> : IRPCChannel
    {
        public IRPCServer Server { get; set; }
        public ReplicationModel Model => Serializer.Model;
        public readonly IReplicateSerializer Serializer;
        Dictionary<MethodKey, RPCContract> serverCache = new Dictionary<MethodKey, RPCContract>();

        public RPCChannel(IReplicateSerializer serializer)
        {
            Serializer = serializer;
        }

        public abstract Stream GetStream(TWireType wireValue);
        public abstract TWireType GetWireValue(Stream stream);
        public abstract Task<Stream> Request(RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        Task<object> IRPCChannel.Request(RPCRequest request, ReliabilityMode reliability)
        {
            return Request(request, reliability).ContinueWith(
                t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception is AggregateException a) throw a.InnerExceptions[0];
                        throw t.Exception;

                    }
                    return Serializer.Deserialize(request.Contract.ResponseType, t.Result);
                });
        }
        public bool TryGetContract(MethodKey endpoint, out RPCContract contract)
        {
            contract = default;
            if (!serverCache.ContainsKey(endpoint))
                serverCache = Server.Methods.ToDictionary(m => m, m => new RPCContract(Model.GetMethod(m)));
            if (serverCache.TryGetValue(endpoint, out contract))
            {
                return true;
            }
            return false;
        }

        public RPCRequest CreateRequest(MethodKey endpoint, TWireType request, ReplicateId? target, out RPCContract contract)
        {
            if (!TryGetContract(endpoint, out contract))
                throw new ContractNotFoundError(endpoint.ToString());
            return new RPCRequest()
            {
                Endpoint = endpoint,
                Contract = contract,
                Request = request == null ? null : Serializer.Deserialize(contract.RequestType, GetStream(request)),
                Target = target,
            };
        }

        public virtual async Task<TWireType> Receive(MethodKey endpoint, TWireType request, ReplicateId? target = null)
        {
            var rpcRequest = CreateRequest(endpoint, request, target, out var contract);
            return GetWireValue(Serializer.Serialize(contract.ResponseType, (await Server.Handle(rpcRequest))));
        }
    }
}
