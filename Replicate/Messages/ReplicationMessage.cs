using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct ReplicationMessage
    {
        public ReplicateId Id;
        // TODO: Fix
        //public DefferedBlob Value;
    }
}
