using Replicate.Interfaces;
using Replicate.Messages;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [Flags]
    public enum ReliabilityMode
    {
        ReliableSequenced = Reliable | Sequenced,
        Reliable = 0b0001,
        Sequenced = 0b0010,
    }

    public delegate Task<object> HandlerDelegate(RPCRequest request);

    public struct RPCRequest
    {
        public RPCContract Contract;
        public object Request;
        /// <summary>
        /// Target may be null if not in an instance RPC
        /// </summary>
        public ReplicatedID? Target;
    }
    public struct RPCContract
    {
        public Type RequestType;
        public Type ResponseType;

        public RPCContract(Type requestType, Type responseType)
        {
            RequestType = requestType;
            ResponseType = responseType.GetTaskReturnType();
        }
        public RPCContract(MethodInfo method)
        {
            var parameters = method.GetParameters();
            RequestType = parameters.Length == 1 ? parameters[0].ParameterType : typeof(None);
            ResponseType = method.ReturnType.GetTaskReturnType();
        }
    }
    struct HandlerInfo
    {
        public RPCContract Contract;
        public HandlerDelegate Handler;
    }
    public interface IReplicationChannel
    {
        Task<object> Request(MethodInfo method, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        void Respond(MethodInfo method, HandlerDelegate handler);
    }
    public static class ChannelExtensions
    {
        public static void Respond(this IReplicationChannel channel, HandlerDelegate handler)
        {
            channel.Respond(handler.Method, handler);
        }
        public static void Respond<TRequest, TResponse>(this IReplicationChannel channel, Func<TRequest, Task<TResponse>> handler)
        {
            channel.Respond(handler.Method, async (req) => { return await handler((TRequest)req.Request); });
        }
        public static void Respond<TRequest>(this IReplicationChannel channel, Action<TRequest> handler)
        {
            channel.Respond(handler.Method, (req) => { handler((TRequest)req.Request); return null; });
        }
        /// <summary>
        /// Avoid using this since there is no type checking on request/response
        /// </summary>
        public static Task<object> Request(this IReplicationChannel channel, MethodInfo method, object request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return channel.Request(method, new RPCRequest()
            {
                Contract = new RPCContract(method),
                Request = request
            }, reliability);
        }

        public static void RegisterSingleton<T>(this IReplicationChannel channel, T implementation)
        {
            foreach (var method in typeof(T).GetMethods())
                channel.Respond(method, (request) => Util.RPCInvoke(method, implementation, request.Request));
        }

        public static T CreateProxy<T>(this IReplicationChannel channel, ReplicatedID? target = null) where T : class
        {
            return ProxyImplement.HookUp<T>(new ReplicatedProxy(target, channel, typeof(T)));
        }
    }

    public abstract class ReplicationChannel<TEndpoint, TWireType> : IReplicationChannel where TEndpoint : class
    {
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
        public virtual async Task<TWireType> Receive(TEndpoint endpoint, TWireType request, ReplicatedID? target = null)
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
