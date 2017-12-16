using System;
using System.Collections.Generic;
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
    public interface IReplicationChannel
    {
        ushort LocalID { get; }
        /// <summary>
        /// Specifies whether or not the channel is allowed to send/receive messages.
        /// When IsOpen is true <see cref="LocalID"/> must be valid.
        /// </summary>
        bool IsOpen { get; }
        Task<byte[]> Poll();
        void Send(ushort? destination, byte[] message, ReliabilityMode reliability);
    }
}
