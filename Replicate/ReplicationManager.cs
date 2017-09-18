using ProtoBuf;
using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    struct ReplicationData
    {
        public object replicated;
        public List<object> targets;
        public List<MetaData.ReplicationData> typeData;
    }
    public class ReplicationManager
    {
        public ReplicationModel Model { get; set; }
        Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        Dictionary<object, uint> objectToIdLookup = new Dictionary<object, uint>();
        Dictionary<uint, ReplicationData> idToObjectLookup = new Dictionary<uint, ReplicationData>();
        IReplicationChannel channel;
        const uint REPLICATE_MESSAGE_ID = uint.MaxValue;
        public ReplicationManager(IReplicationChannel channel, ReplicationModel typeModel = null)
        {
            Model = typeModel ?? ReplicationModel.Default;
            this.channel = channel;
            RegisterHandler(REPLICATE_MESSAGE_ID, HandleReplication);
        }

        Task recvTask = null;
        public void PumpMessages()
        {
            while(recvTask == null || recvTask.IsCompleted)
            {
                if (recvTask == null)
                    recvTask = Receive();
                if (recvTask.IsCompleted)
                    recvTask = null;
            }
        }

        public async Task Receive()
        {
            byte[] message = await channel.Poll();
            Handle(message);
        }
        
        public void Send(uint messageID, byte[] message, ReliabilityMode reliability = ReliabilityMode.Reliable | ReliabilityMode.Sequenced)
        {
            MemoryStream stream = new MemoryStream(4 + message.Length);
            stream.Write(BitConverter.GetBytes(messageID), 0, 4);
            stream.Write(message, 0, message.Length);
            channel.Send(stream.ToArray(), reliability);
        }
        public void Send<T>(uint messageID, T message, ReliabilityMode reliability = ReliabilityMode.Reliable | ReliabilityMode.Sequenced)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(messageID), 0, 4);
            Serializer.Serialize(stream, message);
            channel.Send(stream.ToArray(), reliability);
        }

        public void RegisterHandler(uint messageID, Action<byte[]> handler)
        {
            handlerLookup[messageID] = handler;
        }
        public void RegisterHandler<T>(uint messageID, Action<T> handler)
        {
            handlerLookup[messageID] = (bytes) => handler(Serializer.Deserialize<T>(new MemoryStream(bytes)));
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

        public virtual void HandleReplication(byte[] message)
        {

        }

        public virtual void Replicate(object replicated)
        {

        }

        public virtual List<object> GetReplicationTargets(object replicated)
        {
            return new List<object> { replicated };
        }

        public virtual void RegisterObject(object replicated)
        {
            uint id = AllocateObjectID(replicated);
            objectToIdLookup[replicated] = id;
            var targets = GetReplicationTargets(replicated);
            idToObjectLookup[id] = new ReplicationData()
            {
                replicated = replicated,
                targets = targets,
                typeData = targets.Select(target => Model[target.GetType()]).ToList()
            };
        }

        protected uint autoIncrementId = 1;
        public virtual uint AllocateObjectID(object replicated)
        {
            return autoIncrementId++;
        }

        public virtual uint GetObjectID(object replicated)
        {
            return objectToIdLookup[replicated];
        }

        public virtual object GetObjectFromID(uint id)
        {
            return idToObjectLookup[id];
        }
    }
}
