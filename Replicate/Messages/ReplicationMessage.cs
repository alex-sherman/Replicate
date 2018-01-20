using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public struct ReplicationMessage
    {
        [Replicate]
        public ReplicatedID id;
        [Replicate]
        public byte[] value;
    }
}
