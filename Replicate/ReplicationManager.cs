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
    public struct ReplicatedObject
    {
        public ReplicatedID id;
        public object replicated;
        public TypeAccessor typeAccessor;
    }
    public class ReplicationManager
    {
        public Serializer Serializer { get; protected set; }
        public ReplicationModel Model { get { return Serializer.Model; } }
        public ushort ID { get { return replicationChannel.LocalID; } }
        Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        public Dictionary<object, ReplicatedObject> objectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicatedID, ReplicatedObject> idLookup = new Dictionary<ReplicatedID, ReplicatedObject>();
        List<ReplicatedInterface> rpcInterfaceLookup = new List<ReplicatedInterface>();
        ReplicationChannel replicationChannel;
        public ReplicationManager(Serializer serializer, ReplicationChannel channel)
        {
            replicationChannel = channel;
            this.Serializer = serializer;
            RegisterHandler<ReplicationMessage>(MessageIDs.REPLICATE, HandleReplication);
            RegisterHandler<InitMessage>(MessageIDs.INIT, HandleInit);
            RegisterHandler<RPCMessage>(MessageIDs.RPC, HandleRPC);
        }

        Task recvTask = null;
        public void PumpMessages()
        {
            while (replicationChannel.MessageQueue.Any())
                handleMessage(replicationChannel.MessageQueue.Dequeue());
        }
        public void PumpMessagesAsync()
        {
            while (recvTask == null || recvTask.IsCompleted)
            {
                if (recvTask == null)
                    recvTask = ReceiveAsync();
#if REPMSGTHROW
                if (recvTask.Exception != null)
                    ExceptionDispatchInfo.Capture(recvTask.Exception.InnerExceptions[0]).Throw();
#endif
                if (recvTask.IsCompleted)
                    recvTask = null;
            }
        }

        private async Task ReceiveAsync()
        {
            /// TODO: Receive from all peers
            byte[] message = await replicationChannel.Poll();
            handleMessage(message);
        }

        private void handleMessage(byte[] message)
        {
            ReplicateContext.Current = new ReplicateContext()
            {
                Client = BitConverter.ToUInt16(message, 0),
                Manager = this,
                Model = Model
            };
            byte messageID = message[2];
            if (handlerLookup.ContainsKey(messageID))
            {
                byte[] body = new byte[message.Length - 3];
                Array.Copy(message, 3, body, 0, body.Length);
                handlerLookup[messageID](body);
            }
            ReplicateContext.Clear();
        }

        public void Send<T>(byte messageID, T message, ushort? destination = null, ReliabilityMode reliability = ReliabilityMode.Reliable | ReliabilityMode.Sequenced)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(replicationChannel.LocalID), 0, 2);
            stream.Write(BitConverter.GetBytes(messageID), 0, 1);
            Serializer.Serialize(stream, message);
            replicationChannel.Send(destination, stream.ToArray(), reliability);
        }

        public ReplicationManager RegisterHandler(uint messageID, Action<byte[]> handler)
        {
            handlerLookup[messageID] = handler;
            return this;
        }
        public ReplicationManager RegisterHandler<T>(uint messageID, Action<T> handler)
        {
            handlerLookup[messageID] = (bytes) => handler(Serializer.Deserialize<T>(new MemoryStream(bytes)));
            return this;
        }

        public T CreateProxy<T>(T target) where T : class
        {
            if (!objectLookup.ContainsKey(target))
                throw new InvalidOperationException("Cannot create a proxy for a non-registered object");

            var repObj = objectLookup[target];
            byte interfaceID = (byte)repObj.typeAccessor.TypeData.ReplicatedInterfaces.FindIndex(repInt => repInt.InterfaceType == typeof(T));
            if (interfaceID == byte.MaxValue)
                throw new InvalidOperationException(
                    string.Format("Cannot find a replicated interface definition of {0} for {1}", typeof(T).FullName, repObj.typeAccessor.Name));
            return ProxyImplement.HookUp<T>(
                new ReplicatedProxy(repObj.id, interfaceID, this, repObj.typeAccessor.TypeData.ReplicatedInterfaces[interfaceID]));
        }

        private void HandleReplication(ReplicationMessage message)
        {
            var metaData = idLookup[message.id];
            Serializer.Deserialize(metaData.replicated, new MemoryStream(message.value), metaData.typeAccessor, null, ReplicationSurrogateReplacement);
        }

        private void HandleInit(InitMessage message)
        {
            var typeData = Model.GetTypeAccessor(Model.GetType(message.typeID));
            AddObject(message.id, typeData.Construct());
        }

        private void HandleRPC(RPCMessage message)
        {
            var target = idLookup[message.ReplicatedID];
            var targetInterface = target.typeAccessor.TypeData.ReplicatedInterfaces[message.InterfaceID];
            targetInterface.Invoke(target.replicated, message.MethodID, message.Args.Select(v => v.value).ToArray());
        }

        Type ReplicationSurrogateReplacement(TypeAccessor ta, MemberAccessor ma)
        {
            if (ma?.Info.GetAttribute<ReplicatePolicyAttribute>()?.AsReference == true)
                return typeof(ReplicatedReference<>).MakeGenericType(ta.Type);
            return null;
        }

        public void Replicate(object replicated, ushort? destination = null)
        {
            ReplicateContext.Current = new ReplicateContext()
            {
                Manager = this,
                Model = Model
            };
            var metaData = objectLookup[replicated];
            MemoryStream innerStream = new MemoryStream();
            Serializer.Serialize(innerStream, replicated, metaData.typeAccessor, null, ReplicationSurrogateReplacement);
            ReplicationMessage message = new ReplicationMessage()
            {
                id = metaData.id,
                value = innerStream.ToArray(),
            };
            Send(MessageIDs.REPLICATE, message, destination);
            ReplicateContext.Clear();
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
                owner = ID,
            };
            AddObject(id, replicated);
            var message = new InitMessage()
            {
                id = id,
                typeID = typeID
            };
            Send(MessageIDs.INIT, message);
        }

        private void AddObject(ReplicatedID id, object replicated)
        {
            var data = new ReplicatedObject()
            {
                id = id,
                replicated = replicated,
                typeAccessor = Model.GetTypeAccessor(replicated.GetType()),
            };
            objectLookup[replicated] = data;
            idLookup[id] = data;
        }

        protected uint autoIncrementId = 1;
        public virtual uint AllocateObjectID(object replicated)
        {
            return autoIncrementId++;
        }
    }
}
