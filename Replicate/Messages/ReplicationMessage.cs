using Replicate.MetaData;
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
        public ReplicateId id;
        public List<ReplicationData> Data;
    }
    [ReplicateType]
    public struct ReplicationData
    {
        public MemberKey MemberKey;
        public object Value;
    }
}
