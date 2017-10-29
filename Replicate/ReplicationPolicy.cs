using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replicate.MetaData;
using Replicate.Serialization;

namespace Replicate
{
    /// <todo>
    /// Should be applied to <see cref="TypeAccessor"/>
    /// 
    /// Be able to specify a policy that allows grouping multiple replicated objects <see cref="ReplicatedObject"/>
    /// together such that updates will be applied to the group at once only. 
    /// 
    /// Be able to specify replication frequency
    /// </todo>
    public struct ReplicationPolicy
    {
        public MarshalMethod MarshalMethod;
        public ReplicationPolicy OverrideWith(ReplicationPolicy @override)
        {
            return new ReplicationPolicy()
            {
                MarshalMethod = @override.MarshalMethod == MarshalMethod.Null ? MarshalMethod : @override.MarshalMethod
            };
        }
    }
}
