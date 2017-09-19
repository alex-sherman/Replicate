using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public static class ReplicateContext
    {
        public static uint Client
        {
            get { return (uint)CallContext.LogicalGetData("Replicate.Client"); }
            internal set { CallContext.LogicalSetData("Replicate.Client", value); }
        }
    }
}
