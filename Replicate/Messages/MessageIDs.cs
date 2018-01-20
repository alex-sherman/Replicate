using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    public static class MessageIDs
    {
        public const byte REPLICATE = byte.MaxValue;
        public const byte INIT = byte.MaxValue - 1;
        public const byte RPC = byte.MaxValue - 2;
    }
}
