using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ProtoContract]
    public struct InitMessage
    {
        [ProtoMember(1)]
        public ReplicatedID id;
        [ProtoMember(2)]
        public string typeName;
    }
}
