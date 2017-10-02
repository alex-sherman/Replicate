using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public struct ReplicatedID
    {
        [Replicate]
        public ushort owner;
        [Replicate]
        public uint objectId;

        public override int GetHashCode()
        {
            return (23 * 31 + owner.GetHashCode()) * 31 + objectId.GetHashCode();
        }
    }
}
