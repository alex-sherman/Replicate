using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Interfaces
{
    public class ReplicatedProxy : IImplementor
    {
        object target;
        ReplicationManager manager;
        public ReplicatedProxy(object target, ReplicationManager manager)
        {
            this.target = target;
            this.manager = manager;
        }
        public object Intercept(MethodInfo method, object[] args)
        {
            return method.Invoke(target, args);
        }
    }
}
