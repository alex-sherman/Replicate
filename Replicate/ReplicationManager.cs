using Replicate.MetaTyping;
using Replicate.Messages;
using Replicate.MetaData;
using Replicate.RPC;
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
        public ReplicateId id;
        public object replicated;
        public HashSet<ushort> relevance;
        public TypeAccessor typeAccessor;
    }
    [ReplicateType(AutoMembers = AutoAdd.None, AutoMethods = AutoAdd.None)]
    public class ReplicationManager
    {
        public ReplicationModel Model { get; protected set; }
        protected Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        public Dictionary<object, ReplicatedObject> ObjectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicateId, ReplicatedObject> IDLookup = new Dictionary<ReplicateId, ReplicatedObject>();
        protected Dictionary<Type, object> InterfaceLookup = new Dictionary<Type, object>();
        public IRPCChannel Channel;
        public ReplicationManager(IRPCChannel channel, ReplicationModel model = null)
        {
            Model = model ?? ReplicationModel.Default;
            Channel = channel;
            Channel.Server.Respond<ReplicationMessage>(HandleReplication);
            Channel.Server.Respond<InitMessage>(HandleInit);
            foreach (var method in Model.Where(typeData => typeData.IsInstanceRPC).SelectMany(typeData => typeData.Methods))
                Channel.Server.Respond(method, TypeUtil.CreateHandler(method, request => IDLookup[request.Target.Value].replicated));
        }

        ReplicateContext CreateContext()
        {
            return new ReplicateContext()
            {
                Manager = this,
            };
        }

        public T CreateProxy<T>(T target = null) where T : class
        {
            ReplicateId? id = null;
            if (target != null)
            {
                if (!ObjectLookup.ContainsKey(target))
                    throw new ReplicateError("Cannot create a proxy for a non-registered object");

                id = ObjectLookup[target].id;
            }
            return Channel.CreateProxy<T>(id);
        }

        [ReplicateRPC]
        private void HandleReplication(ReplicationMessage message)
        {
            var metaData = IDLookup[message.Id];
            // TODO: Fix
            //message.Value.ReadInto(metaData.replicated);
        }

        [ReplicateRPC]
        private void HandleInit(InitMessage message)
        {
            var typeData = Model.GetTypeAccessor(Model.GetType(message.typeID));
            AddObject(message.id, typeData.Construct());
        }

        public Task Replicate(object replicated, ushort? destination = null)
        {
            using (ReplicateContext.UsingContext(CreateContext()))
            {
                var metaData = ObjectLookup[replicated];
                ReplicationMessage message = new ReplicationMessage()
                {
                    Id = metaData.id,
                    // TODO: Fix
                    //Value = replicated
                };
                return Channel.Request(((Action<ReplicationMessage>)HandleReplication).Method, message);
            }
        }

        public virtual Task RegisterObject(object replicated)
        {
            var type = replicated.GetType();
            if (!Model.TryGetTypeData(type, out var typeData))
                throw new InvalidOperationException($"Cannot register an object with unknown type: {type}");
            if (typeData.Surrogate != null)
                throw new InvalidOperationException("Cannot register objects which have surrogates");
            TypeId typeID;
            try
            {
                typeID = Model.GetId(replicated.GetType());
            }
            catch (KeyNotFoundException e) { throw new InvalidOperationException($"Cannot register an object with unknown generic type parameter: {e.Source}"); }
            uint objectId = AllocateObjectID(replicated);
            var id = new ReplicateId()
            {
                ObjectID = objectId,
                //creator = ID,
            };
            AddObject(id, replicated);
            var message = new InitMessage()
            {
                id = id,
                typeID = typeID
            };
            return Channel.Request(((Action<InitMessage>)HandleInit).Method, message);
        }

        private void AddObject(ReplicateId id, object replicated)
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
