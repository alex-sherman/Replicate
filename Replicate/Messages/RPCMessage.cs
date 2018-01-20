using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public class RPCMessage
    {
        [Replicate]
        public ReplicatedID ReplicatedID;
        [Replicate]
        public byte InterfaceID;
        [Replicate]
        public byte MethodID;
        [Replicate]
        public List<TypedValue> Args;
    }
}
