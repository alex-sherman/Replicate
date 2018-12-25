using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct InitMessage
    {
        public ReplicatedId id;
        public TypeID typeID;
    }
}
