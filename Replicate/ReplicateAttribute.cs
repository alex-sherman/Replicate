using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class ReplicateAttribute : System.Attribute
    {
        public MarshalMethod? MarshalMethod;
        public ReplicateAttribute(MarshalMethod marshalMethod)
        {
            MarshalMethod = marshalMethod;
        }
        public ReplicateAttribute() { }
    }
}
