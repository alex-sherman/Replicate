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
    public abstract class ReplicationManager
    {
        public ReplicationModel Model { get; protected set; }
        protected Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        public Dictionary<object, ReplicatedObject> ObjectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicatedID, ReplicatedObject> IDLookup = new Dictionary<ReplicatedID, ReplicatedObject>();
        protected Dictionary<Type, object> InterfaceLookup = new Dictionary<Type, object>();
        public abstract Task<object> Publish(MethodInfo method, RPCRequest request);
        public abstract void Subscribe(MethodInfo method, HandlerDelegate handler);

        #region RPC Interfaces
        static Task<object> invoke(MethodInfo method, object target, object request)
        {
            using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true))
            {
                object[] args = new object[] { };
                if (method.GetParameters().Length == 1)
                    args = new[] { request };
                var result = method.Invoke(target, args);
                return TaskUtil.Taskify(method.ReturnType, result);
            }
        }

        public void RegisterInstanceInterface<T>()
        {
            foreach (var method in typeof(T).GetMethods())
            {
                // TODO: This could be done with Reflection.Emit I think?
                Subscribe(method, (request) => invoke(method, IDLookup[request.Target.Value].replicated, request.Request));
            }
        }

        public void RegisterSingleton<T>(T implementation)
        {
            foreach (var method in typeof(T).GetMethods())
            {
                // TODO: This could be done with Reflection.Emit I think?
                Subscribe(method, (request) => invoke(method, implementation, request.Request));
            }
        }
        #endregion
    }
    public class ReplicationManager<TEndpoint> : ReplicationManager where TEndpoint : class
    {
        public ReplicationChannel<TEndpoint> Channel;
        public ReplicationManager(ReplicationChannel<TEndpoint> channel, ReplicationModel model = null)
        {
            Model = model ?? ReplicationModel.Default;
            Channel = channel;
            Channel.Subscribe<ReplicationMessage>(HandleReplication);
            Channel.Subscribe<InitMessage>(HandleInit);
        }

        ReplicateContext CreateContext()
        {
            return new ReplicateContext()
            {
                Manager = this,
            };
        }
        public override Task<object> Publish(MethodInfo method, RPCRequest request)
        {
            return Channel.Publish(Channel.GetEndpoint(method), request);
        }

        public override void Subscribe(MethodInfo method, HandlerDelegate handler)
        {
            Channel.Subscribe(method, handler);
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
            foreach (var data in message.Data)
                metaData.typeAccessor.MemberAccessors[data.MemberID].SetValue(metaData.replicated, data.Value);
        }

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
                    id = metaData.id,
                    Data = metaData.typeAccessor.MemberAccessors.Select(member => new ReplicationData()
                    {
                        MemberID = member.Info.ID,
                        Value = member.GetValue(replicated),
                    }).ToList()
                };
                return Channel.Publish(Channel.GetEndpoint(((Action<ReplicationMessage>)HandleReplication).Method), message);
            }
        }

        public virtual Task RegisterObject(object replicated)
        {
            if (Model.GetTypeAccessor(replicated.GetType()).TypeData.Surrogate != null)
                throw new InvalidOperationException("Cannot register objects which have surrogates");
            var typeID = Model.GetID(replicated.GetType());
            if (typeID.id == ushort.MaxValue)
                throw new InvalidOperationException("Cannot register non [ReplicateType] objects");
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
            return Channel.Publish(Channel.GetEndpoint(((Action<InitMessage>)HandleInit).Method), message);
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
