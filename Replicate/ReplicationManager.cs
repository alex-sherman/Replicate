using Replicate.Interfaces;
using Replicate.Messages;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Replicate
{
    public class ReplicatedObject
    {
        public ReplicatedID id;
        public object replicated;
        public HashSet<ushort> relevance;
        public TypeAccessor typeAccessor;
    }
    public class ReplicationManager
    {
        public Serializer Serializer { get; protected set; }
        public ReplicationModel Model { get { return Serializer.Model; } }
        Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        public Dictionary<object, ReplicatedObject> ObjectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicatedID, ReplicatedObject> IDLookup = new Dictionary<ReplicatedID, ReplicatedObject>();
        List<ReplicatedInterface> rpcInterfaceLookup = new List<ReplicatedInterface>();
        public IReplicationChannel Channel;
        public Implementor ProxyImplementor;
        public ReplicationManager(Serializer serializer, IReplicationChannel channel)
        {
            ProxyImplementor = new Implementor(ProxyTarget);
            Channel = channel;
            Serializer = serializer;
            //RegisterHandler<ReplicationMessage>(MessageIDs.REPLICATE, HandleReplication);
            //RegisterHandler<InitMessage>(MessageIDs.INIT, HandleInit);
            //RegisterHandler<RPCMessage>(MessageIDs.RPC, HandleRPC);
        }

        ReplicateContext CreateContext()
        {
            return new ReplicateContext()
            {
                Serializer = Serializer,
                Manager = this,
            };
        }

        public T CreateProxy<T>(T target) where T : class
        {
            if (!ObjectLookup.ContainsKey(target))
                throw new InvalidOperationException("Cannot create a proxy for a non-registered object");

            var repObj = ObjectLookup[target];
            byte interfaceID = (byte)repObj.typeAccessor.TypeData.ReplicatedInterfaces.FindIndex(repInt => repInt.InterfaceType == typeof(T));
            if (interfaceID == byte.MaxValue)
                throw new InvalidOperationException(
                    string.Format("Cannot find a replicated interface definition of {0} for {1}", typeof(T).FullName, repObj.typeAccessor.Name));
            return ProxyImplement.HookUp<T>(new Implementor(ProxyTarget));
        }
        private object ProxyTarget(MethodInfo method, object[] args)
        {
            return null;
            //Channel.Publish
            //Send(MessageIDs.RPC, new RPCMessage()
            //{
            //    ReplicatedID = Target,
            //    InterfaceID = InterfaceID,
            //    MethodID = ReplicatedInterface.GetMethodID(method),
            //    Args = args.Select(arg => new TypedValue(arg)).ToList(),
            //});
        }
        private void HandleReplication(ReplicationMessage message)
        {
            var metaData = IDLookup[message.id];
            Serializer.Deserialize(metaData.replicated, new MemoryStream(message.value), metaData.typeAccessor, null, ReplicationSurrogateReplacement);
        }

        private void HandleInit(InitMessage message)
        {
            var typeData = Model.GetTypeAccessor(Model.GetType(message.typeID));
            AddObject(message.id, typeData.Construct());
        }

        private void HandleRPC(RPCMessage message)
        {
            var target = IDLookup[message.ReplicatedID];
            var targetInterface = target.typeAccessor.TypeData.ReplicatedInterfaces[message.InterfaceID];
            using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true))
                targetInterface.Invoke(target.replicated, message.MethodID, message.Args.Select(v => v.value).ToArray());
        }

        Type ReplicationSurrogateReplacement(object obj, MemberAccessor ma)
        {
            if (ma?.Info.GetAttribute<ReplicatePolicyAttribute>()?.AsReference == true)
                return typeof(ReplicatedReference<>).MakeGenericType(ma.Type);
            return null;
        }

        public void Replicate(object replicated, ushort? destination = null)
        {
            using (ReplicateContext.UsingContext(CreateContext()))
            {
                var metaData = ObjectLookup[replicated];
                MemoryStream innerStream = new MemoryStream();
                Serializer.Serialize(innerStream, replicated, metaData.typeAccessor, null, ReplicationSurrogateReplacement);
                ReplicationMessage message = new ReplicationMessage()
                {
                    id = metaData.id,
                    value = innerStream.ToArray(),
                };
                //Send(MessageIDs.REPLICATE, message, destination);
            }
        }

        public virtual void RegisterObject(object replicated)
        {
            if (Model.GetTypeAccessor(replicated.GetType()).TypeData.Surrogate != null)
                throw new InvalidOperationException("Cannot register objects which have surrogates");
            var typeID = Model.GetID(replicated.GetType());
            if (typeID.id == ushort.MaxValue)
                throw new InvalidOperationException("Cannot register non [Replicate] objects");
            uint objectId = AllocateObjectID(replicated);
            var id = new ReplicatedID()
            {
                objectId = objectId,
                //creator = ID,
            };
            AddObject(id, replicated);
            var message = new InitMessage()
            {
                id = id,
                typeID = typeID
            };
            //Send(MessageIDs.INIT, message);
        }

        private void AddObject(ReplicatedID id, object replicated)
        {
            var data = new ReplicatedObject()
            {
                id = id,
                replicated = replicated,
                typeAccessor = Model.GetTypeAccessor(replicated.GetType()),
            };
            ObjectLookup[replicated] = data;
            IDLookup[id] = data;
        }

        protected uint autoIncrementId = 1;
        public virtual uint AllocateObjectID(object replicated)
        {
            return autoIncrementId++;
        }
    }
}
