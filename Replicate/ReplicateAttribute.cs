using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReplicateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReplicateIgnoreAttribute : Attribute { }

    public enum AutoAdd
    {
        None,
        AllPublic,
        All,
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class ReplicateTypeAttribute : Attribute
    {
        public Type SurrogateType;
        public AutoAdd AutoMembers = AutoAdd.AllPublic;
        public AutoAdd AutoMethods = AutoAdd.None;
        public bool IsInstanceRPC = false;
    }
}
