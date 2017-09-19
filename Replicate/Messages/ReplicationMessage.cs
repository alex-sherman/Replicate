using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ProtoContract]
    public struct ReplicationTargetData
    {
        [ProtoMember(1, IsRequired = false)]
        public byte? objectIndex;
        [ProtoMember(2)]
        public byte[] value;
    }
    [ProtoContract]
    public struct ReplicationMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public ReplicatedID id;
        [ProtoMember(2, IsRequired = true)]
        public List<ReplicationTargetData> members;
    }
}
