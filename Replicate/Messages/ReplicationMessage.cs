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
        public ReplicatedID id;
        public List<ReplicationData> Data;
    }
    public struct ReplicationData
    {
        public byte MemberID;
        public object Value;
    }
}
