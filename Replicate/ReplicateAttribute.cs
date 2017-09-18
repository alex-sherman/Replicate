using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
    public class ReplicateAttribute : System.Attribute
    {
        public ReplicateAttribute() { }
    }
}
