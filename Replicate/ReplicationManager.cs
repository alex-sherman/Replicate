using ProtoBuf;
using Replicate.Messages;
using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public struct ReplicatedObject
    {
        public ReplicatedID id;
        public object replicated;
        public List<Tuple<object, TypeData>> targets;
    }
    public class ReplicationManager
    {
        private static class MessageIDs
        {
            public const uint REPLICATE = uint.MaxValue;
            public const uint INIT = uint.MaxValue - 1;
        }
        public ReplicationModel Model { get; set; }
        public ushort? ID = null;
        Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        Dictionary<object, ReplicatedObject> objectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicatedID, ReplicatedObject> idLookup = new Dictionary<ReplicatedID, ReplicatedObject>();
        Dictionary<ushort, IReplicationChannel> peers = new Dictionary<ushort, IReplicationChannel>();
        public ReplicationManager(ReplicationModel typeModel = null)
        {
            Model = typeModel ?? ReplicationModel.Default;
            RegisterHandler<ReplicationMessage>(MessageIDs.REPLICATE, HandleReplication);
            RegisterHandler<InitMessage>(MessageIDs.INIT, HandleInit);
        }

        Task recvTask = null;
        public void PumpMessages()
        {
            while(recvTask == null || recvTask.IsCompleted)
            {
                if (recvTask == null)
                    recvTask = Receive();
#if REPMSGTHROW
                if (recvTask.Exception != null)
                    throw recvTask.Exception.InnerExceptions[0];
#endif
                if (recvTask.IsCompleted)
                    recvTask = null;
            }
        }

        public async Task Receive()
        {
            /// TODO: Receive from all peers
            byte[] message = await peers.Values.First().Poll();
            ReplicateContext.Client = peers.Keys.First();
            Handle(message);
        }

        private void SendBytes(MemoryStream stream, ushort? destination, ReliabilityMode reliability)
        {
            if (destination.HasValue)
                peers[destination.Value].Send(stream.ToArray(), reliability);
            else
                foreach (var peer in peers.Values)
                    peer.Send(stream.ToArray(), reliability);
        }

        public void Send(uint messageID, byte[] message, ushort? destination = null, ReliabilityMode reliability = ReliabilityMode.Reliable | ReliabilityMode.Sequenced)
        {
            MemoryStream stream = new MemoryStream(4 + message.Length);
            stream.Write(BitConverter.GetBytes(messageID), 0, 4);
            stream.Write(message, 0, message.Length);
            SendBytes(stream, destination, reliability);
        }

        public void Send<T>(uint messageID, T message, ushort? destination = null, ReliabilityMode reliability = ReliabilityMode.Reliable | ReliabilityMode.Sequenced)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(messageID), 0, 4);
            Serializer.Serialize(stream, message);
            SendBytes(stream, destination, reliability);
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

        public ReplicationManager RegisterClient(ushort id, IReplicationChannel channel)
        {
            peers[id] = channel;
            return this;
        }

        public virtual void Handle(byte[] message)
        {
            uint messageID = BitConverter.ToUInt32(message, 0);
            if (handlerLookup.ContainsKey(messageID))
            {
                byte[] body = new byte[message.Length - 4];
                Array.Copy(message, 4, body, 0, body.Length);
                handlerLookup[messageID](body);
            }
        }

        public virtual void HandleReplication(ReplicationMessage message)
        {
            var metaData = idLookup[message.id];
        }

        private void HandleInit(InitMessage message)
        {
            var typeData = Model[message.typeName];
            AddObject(message.id, typeData.Construct());
        }

        public virtual void Replicate(object replicated, ushort? destination = null)
        {
            var metaData = objectLookup[replicated];
            ReplicationMessage message = new ReplicationMessage()
            {
                id = metaData.id,
                members = metaData.targets.Select((target, i) => new ReplicationTargetData()
                {
                    objectIndex = (byte)i,
                    value = target.Item2.GetBytes(target.Item1)
                }).ToList()
            };
            Send(MessageIDs.REPLICATE, message, destination);
        }

        public virtual List<object> GetReplicationTargets(object replicated)
        {
            return new List<object> { replicated };
        }

        public virtual void RegisterObject(object replicated)
        {
            uint objectId = AllocateObjectID(replicated);
            var id = new ReplicatedID()
            {
                objectId = objectId,
                owner = ID.Value,
            };
            AddObject(id, replicated);
            var message = new InitMessage()
            {
                id = id,
                typeName = Model[replicated.GetType()].Name
            };
            Send(MessageIDs.INIT, message);
        }

        private void AddObject(ReplicatedID id, object replicated)
        {
            var targets = GetReplicationTargets(replicated);
            var data = new ReplicatedObject()
            {
                id = id,
                replicated = replicated,
                targets = targets.Select(
                    target => new Tuple<object, TypeData>(target, Model[target.GetType()])
                ).ToList()
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
