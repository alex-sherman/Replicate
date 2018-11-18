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
        Dictionary<Type, object> InterfaceLookup = new Dictionary<Type, object>();
        public IReplicationChannel Channel;
        public ReplicationManager(Serializer serializer, IReplicationChannel channel)
        {
            Channel = channel;
            Serializer = serializer;
            Channel.Subscribe<ReplicationMessage>(HandleReplication);
            Channel.Subscribe<InitMessage>(HandleInit);
            Channel.Subscribe<RPCMessage, TypedValue>(HandleRPC);
        }

        ReplicateContext CreateContext()
        {
            return new ReplicateContext()
            {
                Serializer = Serializer,
                Manager = this,
            };
        }

        public T CreateProxy<T>(T target = null) where T : class
        {
            ReplicatedID? id = null;
            if (target != null)
            {
                if (!ObjectLookup.ContainsKey(target))
                    throw new InvalidOperationException("Cannot create a proxy for a non-registered object");

                id = ObjectLookup[target].id;
            }
            return ProxyImplement.HookUp<T>(new ReplicatedProxy(id, this, typeof(T)));
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

        private async Task<TypedValue> HandleRPC(RPCMessage message)
        {
            object target = message.ReplicatedID.HasValue ?
                IDLookup[message.ReplicatedID.Value].replicated :
                InterfaceLookup[message.InterfaceType];

            using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true))
            {
                var resType = message.Method.ReturnType;
                var response = message.Method.Invoke(target, message.Args.Select(v => v.Value).ToArray());
                if(response is Task task)
                {
                    await task;
                    if (resType == typeof(Task))
                        return new TypedValue();
                    if (resType.IsGenericType && resType.GetGenericTypeDefinition() == typeof(Task<>))
                        // TODO: This takes like 200ms sometimes, could probably use Emit to fix this?
                        return new TypedValue((object)((dynamic)task).Result);
                }
                return new TypedValue(response);
            }
        }

        Type ReplicationSurrogateReplacement(object obj, MemberAccessor ma)
        {
            if (ma?.Info.GetAttribute<ReplicatePolicyAttribute>()?.AsReference == true)
                return typeof(ReplicatedReference<>).MakeGenericType(ma.Type);
            return null;
        }

        public Task Replicate(object replicated, ushort? destination = null)
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
                return Channel.Publish(message);
            }
        }

        public virtual Task RegisterObject(object replicated)
        {
            if (Model.GetTypeAccessor(replicated.GetType()).TypeData.Surrogate != null)
                throw new InvalidOperationException("Cannot register objects which have surrogates");
            var typeID = Model.GetID(replicated.GetType());
            if (typeID.id == ushort.MaxValue)
                throw new InvalidOperationException("Cannot register non [Replicate] objects");
            uint objectId = AllocateObjectID(replicated);
            var id = new ReplicatedID()
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
            return Channel.Publish(message);
        }

        public void RegisterSingleton<T>(T implementation)
        {
            InterfaceLookup[typeof(T)] = implementation;
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
