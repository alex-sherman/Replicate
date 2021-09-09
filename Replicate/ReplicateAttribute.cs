using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReplicateAttribute : Attribute
    {
        public RepKey? Key;
        public ReplicateAttribute(int key) { Key = key; }
        public ReplicateAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ReplicateIgnoreAttribute : Attribute { }

    public enum AutoAdd
    {
        None,
        AllPublic,
        All,
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum)]
    public class ReplicateTypeAttribute : Attribute
    {
        public Type SurrogateType;
        public AutoAdd AutoMembers = AutoAdd.AllPublic;
        public AutoAdd AutoMethods = AutoAdd.AllPublic;
        public bool IsInstanceRPC = false;
    }
}
