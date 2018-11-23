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
        public Type RequestType;
        public Type ResponseType;
        public object Request;
        /// <summary>
        /// Method may be null if not in RPC
        /// </summary>
        public MethodInfo Method;
        /// <summary>
        /// Target may be null if not in an instance RPC
        /// </summary>
        public ReplicatedID? Target;
    }

    public abstract class ReplicationChannel<TEndpoint>
    {
        // TODO: Could add type data to responders to more quickly check if casts are valid
        Dictionary<TEndpoint, HandlerDelegate> responders = new Dictionary<TEndpoint, HandlerDelegate>();
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        public bool IsOpen { get; protected set; }

        public abstract TEndpoint GetEndpoint(MethodInfo endpoint);
        public abstract ReplicatedID? GetRPCTarget(TEndpoint endpoint);

        public abstract Task<object> Publish(TEndpoint messageID, RPCRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        public async Task<TResponse> Publish<TRequest, TResponse>(TEndpoint messageID, TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return (TResponse)await Publish(messageID, new RPCRequest()
            {
                RequestType = typeof(TRequest),
                ResponseType = typeof(TResponse),
                Request = request,
            }, reliability);
        }
        public Task Publish<TRequest>(TEndpoint messageID, TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return Publish<TRequest, None>(messageID, request, reliability);
        }
        public virtual void Subscribe<TRequest>(Action<TRequest> handler)
        {
            Subscribe(GetEndpoint(handler.Method), (req) => { handler((TRequest)req.Request); return null; });
        }
        public void Subscribe(TEndpoint messageID, HandlerDelegate handler)
        {
            responders[messageID] = handler;
        }

        protected virtual async Task<object> Receive(TEndpoint endpoint, RPCRequest request)
        {
            if (responders.TryGetValue(endpoint, out var responder))
            {
                // TODO: Is this explicit cast dangerous? Yes, yes it is.
                var task = responder(request);
                if (task != null)
                    return await task;
            }
            return None.Value;
        }
    }
}
