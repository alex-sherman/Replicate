using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    [Replicate]
    public class ReplicatedReference<T> where T : class
    {
        [Replicate]
        public ReplicatedID replicatedID;
        public static implicit operator T(ReplicatedReference<T> self)
        {
            if (self == null)
                return null;
            return (T)ReplicateContext.Current.Manager.IDLookup[self.replicatedID].replicated;
        }
        public static implicit operator ReplicatedReference<T>(T value)
        {
            if (value == null)
                return null;
            return new ReplicatedReference<T>() { replicatedID = ReplicateContext.Current.Manager.ObjectLookup[value].id };
        }
    }
}
