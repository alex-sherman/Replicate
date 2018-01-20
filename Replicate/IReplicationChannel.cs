using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [Flags]
    public enum ReliabilityMode
    {
        Reliable,
        Sequenced
    }
    public abstract class ReplicationChannel
    {
        public ushort LocalID { get; protected set; }
        public Queue<byte[]> MessageQueue = new Queue<byte[]>();
        private Queue<TaskCompletionSource<byte[]>> sources = new Queue<TaskCompletionSource<byte[]>>();

        public virtual void Put(byte[] item)
        {
            lock (MessageQueue)
            {
                if (sources.Any())
                    sources.Dequeue().SetResult(item);
                else
                    MessageQueue.Enqueue(item);
            }
        }
        public virtual Task<byte[]> Poll()
        {
            lock (MessageQueue)
            {
                if (MessageQueue.Any())
                    return Task.FromResult(MessageQueue.Dequeue());
                var source = new TaskCompletionSource<byte[]>();
                sources.Enqueue(source);
                return source.Task;
            }
        }
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        bool IsOpen { get; }
        public abstract void Send(ushort? destination, byte[] message, ReliabilityMode reliability);
    }
}
