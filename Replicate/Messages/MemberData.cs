using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ProtoContract]
    struct MemberData
    {
        [ProtoMember(1)]
        public int id;
        [ProtoMember(2)]
        public byte[] value;
    }
}
