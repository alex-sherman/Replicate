using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [ReplicateType]
    public class ReplicatedReference<T> where T : class
    {
        [Replicate]
        public ReplicatedId Id;
        public static implicit operator T(ReplicatedReference<T> self)
        {
            if (self == null)
                return null;
            return (T)ReplicateContext.Current.Manager.IDLookup[self.Id].replicated;
        }
        public static implicit operator ReplicatedReference<T>(T value)
        {
            if (value == null)
                return null;
            if (ReplicateContext.Current.Manager == null)
                throw new ReplicatedReferenceError($"Cannot serialize replicate references without an active {nameof(ReplicationManager)}");
            return new ReplicatedReference<T>() { Id = ReplicateContext.Current.Manager.ObjectLookup[value].id };
        }
    }
}
