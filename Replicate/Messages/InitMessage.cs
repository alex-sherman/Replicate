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
        public ReplicatedID id;
        public TypeID typeID;
    }
}
