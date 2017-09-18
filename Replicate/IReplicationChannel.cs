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
        Task<byte[]> Poll();
        void Send(byte[] message, ReliabilityMode reliability);
    }
}
