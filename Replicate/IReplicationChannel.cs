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
        Reliable = 0b0001,
        Sequenced = 0b0010,
    }
    public interface IReplicationChannel
    {
        Task<TResponse> Publish<TRequest, TResponse>(TRequest request, ReliabilityMode reliability);
        void Subscribe<TRequest, TResponse>(Func<TRequest, Task<object>> handler);
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

        public void Subscribe<TRequest, TResponse>(Func<TRequest, Task<object>> handler)
        {
            var messageID = GetMessageID(typeof(TRequest));
            if (typeof(TResponse) == typeof(Void))
            {
                if (!subscribers.TryGetValue(messageID, out var subs))
                    subs = subscribers[messageID] = new List<Action<object>>();
                subs.Add((obj) => handler((TRequest)obj));
            }
            else
                responders[messageID] = (obj) => handler((TRequest)obj);
        }

        protected virtual void Receive<TRequest, TResponse>(TRequest value)
        {
            var messageID = GetMessageID(typeof(TRequest));
            if (subscribers.ContainsKey(messageID))
                foreach (var sub in subscribers[messageID])
                    sub(value);
            if (responders.ContainsKey(messageID))
            {
                responders[messageID](value).ContinueWith(t =>
                {
                    Publish<TResponse, Void>((TResponse)t.Result, ReliabilityMode.Reliable | ReliabilityMode.Sequenced);
                });

            }
        }
    }
}
