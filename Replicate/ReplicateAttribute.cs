using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class ReplicateAttribute : Attribute
    {
    }
}
