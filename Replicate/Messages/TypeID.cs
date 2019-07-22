using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct TypeId
    {
        public ushort id;
        public TypeId[] subtypes;
    }
}
