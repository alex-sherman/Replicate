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
    public interface IReplicationChannel
    {
        Task<TResponse> Publish<TRequest, TResponse>(TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced);
        void Subscribe<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler);
    }

    public static class ReplicationChannelExtensions
    {
        public static void Subscribe<TRequest>(this IReplicationChannel @this, Action<TRequest> handler)
        {
            @this.Subscribe<TRequest, None>(request => { handler(request); return null; });
        }
        public static Task Publish<TRequest>(this IReplicationChannel @this, TRequest request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return @this.Publish<TRequest, None>(request, reliability);
        }
    }

    public abstract class ReplicationChannel<TMessageID> : IReplicationChannel
    {
        Dictionary<TMessageID, List<Action<object>>> subscribers = new Dictionary<TMessageID, List<Action<object>>>();
        Dictionary<TMessageID, Func<object, Task<object>>> responders = new Dictionary<TMessageID, Func<object, Task<object>>>();
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        public bool IsOpen { get; protected set; }

        public abstract TMessageID GetMessageID(Type type);

        public abstract Task<TResponse> Publish<TRequest, TResponse>(TRequest request, ReliabilityMode reliability);

        public void Subscribe<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        {
            var messageID = GetMessageID(typeof(TRequest));
            if (typeof(TResponse) == typeof(None))
            {
                if (!subscribers.TryGetValue(messageID, out var subs))
                    subs = subscribers[messageID] = new List<Action<object>>();
                subs.Add((obj) => handler((TRequest)obj));
            }
            else
                responders[messageID] = async (obj) => await handler((TRequest)obj);
        }

        protected virtual async Task<TResponse> Receive<TRequest, TResponse>(TRequest value)
        {
            var messageID = GetMessageID(typeof(TRequest));
            if (subscribers.ContainsKey(messageID))
                foreach (var sub in subscribers[messageID])
                    sub(value);
            if (responders.ContainsKey(messageID))
                // TODO: Is this explicit cast dangerous?
                return (TResponse)(await responders[messageID](value));
            return default(TResponse);
        }
    }
}
