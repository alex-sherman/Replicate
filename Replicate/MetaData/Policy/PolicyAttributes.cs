using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData.Policy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class AsReferenceAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class NonNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NullableAttribute : Attribute { }
}
