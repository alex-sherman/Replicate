using System;

namespace Replicate.MetaData.Policy {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class AsReferenceAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipEmptyAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class NonNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NullableAttribute : Attribute { }
}
