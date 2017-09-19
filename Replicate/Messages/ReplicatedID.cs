using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ProtoContract]
    public struct ReplicatedID
    {
        [ProtoMember(1, IsRequired = true)]
        public ushort owner;
        [ProtoMember(2, IsRequired = true)]
        public uint objectId;

        public override int GetHashCode()
        {
            return (23 * 31 + owner.GetHashCode()) * 31 + objectId.GetHashCode();
        }
    }
}
