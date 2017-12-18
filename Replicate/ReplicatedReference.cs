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
            var manager = ReplicateContext.Current.Manager;
            return (T)manager.idLookup[self.replicatedID].replicated;
        }
        public static implicit operator ReplicatedReference<T>(T value)
        {
            if (value == null)
                return null;
            var manager = ReplicateContext.Current.Manager;
            return new ReplicatedReference<T>() { replicatedID = manager.objectLookup[value].id };
        }
    }
}
