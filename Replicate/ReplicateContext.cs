using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [Serializable]
    public struct ReplicateContext
    {
        public uint Client { get; internal set; }
        public ReplicationManager Manager { get; internal set; }
        public static ReplicateContext Current
        {
            get { return (ReplicateContext)CallContext.LogicalGetData("Replicate.Context"); }
            internal set { CallContext.LogicalSetData("Replicate.Context", value); }
        }
        internal static void Clear()
        {
            CallContext.FreeNamedDataSlot("Replicate.Context");
        }
    }
}
