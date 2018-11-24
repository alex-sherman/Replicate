using Replicate.Messages;
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
            ResponseType = responseType;
        }
        public RPCContract(MethodInfo method)
        {
            var parameters = method.GetParameters();
            RequestType = parameters.Length == 1 ? parameters[0].ParameterType : typeof(None);
            ResponseType = method.ReturnType;
        }
    }
    struct HandlerInfo
    {
        public RPCContract Contract;
        public HandlerDelegate Handler;
    }
    public abstract class ReplicationChannel<TEndpoint> where TEndpoint : class
    {
        // TODO: Could add type data to responders to more quickly check if casts are valid
        Dictionary<TEndpoint, HandlerInfo> responders = new Dictionary<TEndpoint, HandlerInfo>();
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        public bool IsOpen { get; protected set; }

        public abstract TEndpoint GetEndpoint(MethodInfo endpoint);

        public abstract Task<object> Publish(TEndpoint messageID, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        public async Task<TResponse> Publish<TRequest, TResponse>(TEndpoint messageID, TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            var result = await Publish(messageID, new RPCRequest()
            {
                Contract = new RPCContract(typeof(TRequest), typeof(TResponse)),
                Request = request,
            }, reliability);
            if (typeof(TResponse) == typeof(None))
                return default(TResponse);
            return (TResponse)result;
        }
        public Task Publish<TRequest>(TEndpoint messageID, TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return Publish<TRequest, None>(messageID, request, reliability);
        }

        public void Subscribe<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler, TEndpoint endpoint = null)
        {
            Subscribe(endpoint ?? GetEndpoint(handler.Method),
                async (req) => { return await handler((TRequest)req.Request); },
                new RPCContract(typeof(TRequest), typeof(TResponse)));
        }
        public void Subscribe<TRequest>(Action<TRequest> handler, TEndpoint endpoint = null)
        {
            Subscribe(endpoint ?? GetEndpoint(handler.Method),
                (req) => { handler((TRequest)req.Request); return null; },
                new RPCContract(typeof(TRequest), typeof(None)));
        }
        public void Subscribe(MethodInfo method, HandlerDelegate handler)
        {
            Subscribe(GetEndpoint(method), handler, new RPCContract(method));
        }
        public void Subscribe(TEndpoint messageID, HandlerDelegate handler, RPCContract contract)
        {
            responders[messageID] = new HandlerInfo()
            {
                Handler = handler,
                Contract = contract,
            };
        }

        public bool TryGetContract(TEndpoint endpoint, out RPCContract contract)
        {
            contract = default(RPCContract);
            if (responders.TryGetValue(endpoint, out var handler)){
                contract = handler.Contract;
                return true;
            }
            return false;
        }

        public virtual async Task<object> Receive(TEndpoint endpoint, RPCRequest request)
        {
            if (responders.TryGetValue(endpoint, out var handlerInfo))
            {
                // TODO: Is this explicit cast dangerous? Yes, yes it is.
                var task = handlerInfo.Handler(request);
                if (task != null)
                    return await task;
            }
            return None.Value;
        }
    }
}
