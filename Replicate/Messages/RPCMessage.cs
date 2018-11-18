using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public class RPCMessage
    {
        [Replicate]
        public ReplicatedID? ReplicatedID;
        [Replicate]
        public Type InterfaceType;
        [Replicate]
        public MethodInfo Method;
        [Replicate]
        public List<TypedValue> Args;
    }
}
