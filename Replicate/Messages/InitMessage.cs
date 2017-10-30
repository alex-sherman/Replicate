using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public struct InitMessage
    {
        [Replicate]
        public ReplicatedID id;
        [Replicate]
        public TypeID typeID;
    }
}
