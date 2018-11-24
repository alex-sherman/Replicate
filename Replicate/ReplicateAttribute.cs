using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReplicateAttribute : Attribute { }

    public enum AutoMembers
    {
        None,
        AllPublic,
        All,
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ReplicateTypeAttribute : Attribute
    {
        public Type SurrogateType;
        public AutoMembers AutoMembers;
        public ReplicateTypeAttribute(AutoMembers autoMembers = AutoMembers.AllPublic, Type surrogate = null)
        {
            AutoMembers = autoMembers;
            SurrogateType = surrogate;
        }
    }
}
