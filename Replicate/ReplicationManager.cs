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
        public List<Tuple<object, TypeAccessor>> targets;
    }
    public class ReplicationManager
    {
        public Serializer Serializer { get; protected set; }
        private static class MessageIDs
        {
            public const byte REPLICATE = byte.MaxValue;
            public const byte INIT = byte.MaxValue - 1;
            public const byte RPC = byte.MaxValue - 2;
        }
        public ReplicationModel Model { get { return Serializer.Model; } }
        public ushort ID { get { return replicationChannel.LocalID; } }
        Dictionary<uint, Action<byte[]>> handlerLookup = new Dictionary<uint, Action<byte[]>>();
        public Dictionary<object, ReplicatedObject> objectLookup = new Dictionary<object, ReplicatedObject>();
        public Dictionary<ReplicatedID, ReplicatedObject> idLookup = new Dictionary<ReplicatedID, ReplicatedObject>();
        Dictionary<ushort, Type> idTypeLookup = new Dictionary<ushort, Type>();
        Dictionary<Type, ushort> typeIdLookup = new Dictionary<Type, ushort>();
        Dictionary<ushort, ReplicatedInterface> rpcInterfaceLookup = new Dictionary<ushort, ReplicatedInterface>();
        ushort lastTypeId = 1;
        IReplicationChannel replicationChannel;
        public ReplicationManager(Serializer serializer, IReplicationChannel channel)
        {
            replicationChannel = channel;
            this.Serializer = serializer;
            RegisterHandler<ReplicationMessage>(MessageIDs.REPLICATE, HandleReplication);
            RegisterHandler<InitMessage>(MessageIDs.INIT, HandleInit);
            RegisterType(typeof(byte));
            RegisterType(typeof(ushort));
            RegisterType(typeof(short));
            RegisterType(typeof(uint));
            RegisterType(typeof(int));
            RegisterType(typeof(ulong));
            RegisterType(typeof(long));
            RegisterType(typeof(string));
            RegisterType(typeof(char));
            RegisterType(typeof(List<>));
            RegisterType(typeof(Dictionary<,>));
            RegisterType(typeof(TypedValue));
            Serializer.Model[typeof(TypedValue)].SetSurrogate(typeof(TypedValueSurrogate));
            foreach (var type in Assembly.GetCallingAssembly().GetTypes().OrderBy(_type => _type.FullName))
            {
                var replicate = type.GetCustomAttribute<ReplicateAttribute>();
                if (replicate != null)
                {
                    RegisterType(type);
                }
            }
        }

        Task recvTask = null;
        public void PumpMessages()
        {
            while (recvTask == null || recvTask.IsCompleted)
            {
                if (recvTask == null)
                    recvTask = Receive();
#if REPMSGTHROW
                if (recvTask.Exception != null)
                    ExceptionDispatchInfo.Capture(recvTask.Exception.InnerExceptions[0]).Throw();
#endif
                if (recvTask.IsCompleted)
                    recvTask = null;
            }
        }

        public async Task Receive()
        {
            /// TODO: Receive from all peers
            byte[] message = await replicationChannel.Poll();

            ReplicateContext.Current = new ReplicateContext()
            {
                Client = BitConverter.ToUInt16(message, 0),
                Manager = this
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
        public ReplicationManager RegisterInterface<T>(ushort interfaceID, T handler) where T : class
        {
            rpcInterfaceLookup[interfaceID] = new ReplicatedInterface(handler, typeof(T));
            return this;
        }
        public uint RegisterType(Type type)
        {
            var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            ushort id = lastTypeId++;
            typeIdLookup[type] = id;
            idTypeLookup[id] = type;
            return id;
        }

        public TypeID GetID(Type type)
        {
            var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var output = new TypeID()
            {
                id = typeIdLookup[genericType]
            };
            if (type.IsGenericType)
                output.subtypes = type.GetGenericArguments().Select(t => GetID(t)).ToArray();
            return output;
        }
        public Type GetType(TypeID typeID)
        {
            Type type = idTypeLookup[typeID.id];
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(typeID.subtypes.Select(subType => GetType(subType)).ToArray());
            }
            return type;
        }

        public TypeAccessor GetTypeAccessor(TypeID typeID)
        {
            return Model.GetTypeAccessor(GetType(typeID));
        }

        private void HandleReplication(ReplicationMessage message)
        {
            var metaData = idLookup[message.id];
            foreach (var member in message.members)
            {
                var target = metaData.targets[member.objectIndex];
                Serializer.Deserialize(target.Item1, new MemoryStream(member.value), target.Item2.Type, target.Item2, null, ReplicationSurrogateReplacement);
            }
        }

        private void HandleInit(InitMessage message)
        {
            var typeData = GetTypeAccessor(message.typeID);
            AddObject(message.id, typeData.Construct());
        }

        private void HandleRPC(RPCMessage message)
        {
            if (message.ReplicatedID == null)
            {
                if (rpcInterfaceLookup.TryGetValue(message.InterfaceID, out var target))
                {
                    target.Invoke(message.MethodID, message.Args.Select(v => v.value).ToArray());
                }
            }
        }

        Type ReplicationSurrogateReplacement(TypeAccessor ta, MemberAccessor ma)
        {
            if (ma?.Info.GetAttribute<ReplicatePolicyAttribute>()?.AsReference == true)
                return typeof(ReplicatedReference<>).MakeGenericType(ta.Type);
            return null;
        }

        public void Replicate(object replicated, ushort? destination = null)
        {
            ReplicateContext.Current = new ReplicateContext() { Manager = this };
            var metaData = objectLookup[replicated];
            ReplicationMessage message = new ReplicationMessage()
            {
                id = metaData.id,
                members = metaData.targets.Select((target, i) =>
                {
                    MemoryStream innerStream = new MemoryStream();
                    Serializer.Serialize(innerStream, target.Item1, target.Item2, null, ReplicationSurrogateReplacement);
                    return new ReplicationTargetData()
                    {
                        objectIndex = (byte)i,
                        value = innerStream.ToArray()
                    };
                }).ToList()
            };
            Send(MessageIDs.REPLICATE, message, destination);
            ReplicateContext.Clear();
        }

        public virtual List<object> GetReplicationTargets(object replicated)
        {
            return new List<object> { replicated };
        }

        public virtual void RegisterObject(object replicated)
        {
            if (Model.GetTypeAccessor(replicated.GetType()).TypeData.Surrogate != null)
                throw new InvalidOperationException("Cannot register objects which have surrogates");
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
                typeID = GetID(replicated.GetType())
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
                    target => new Tuple<object, TypeAccessor>(target, Model.GetTypeAccessor(target.GetType()))
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
