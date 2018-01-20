using Replicate.Messages;
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
        ReplicatedID target;
        byte interfaceID;
        ReplicationManager manager;
        ReplicatedInterface replicatedInterface;
        public ReplicatedProxy(ReplicatedID target, byte interfaceID, ReplicationManager manager, ReplicatedInterface replicatedInterface)
        {
            this.target = target;
            this.interfaceID = interfaceID;
            this.manager = manager;
            this.replicatedInterface = replicatedInterface;
        }
        public object Intercept(MethodInfo method, object[] args)
        {
            manager.Send(MessageIDs.RPC, new RPCMessage()
            {
                ReplicatedID = target,
                InterfaceID = interfaceID,
                MethodID = replicatedInterface.GetMethodID(method),
                Args = args.Select(arg => new TypedValue(arg)).ToList(),
            });
            return null;
        }
    }
}
