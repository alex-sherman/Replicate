using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReplicateRPCAttribute : Attribute
    {

    }
}
