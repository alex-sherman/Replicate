using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    public static class BinarySerializerUtil
    {
        public struct ClientServer
        {
            public ReplicationModel model;
            public PassThroughChannel channel;
            public ReplicationManager server;
            public ReplicationManager client;
        }
        public static ClientServer MakeClientServer()
        {
            ReplicationModel model = new ReplicationModel();
            PassThroughChannel channel = new PassThroughChannel();
            channel.SetSerializer(new BinarySerializer(model));
            return new ClientServer()
            {
                model = model,
                channel = channel,
                server = new ReplicationManager(channel.PointA, model),
                client = new ReplicationManager(channel.PointB, model)
            };
        }

        public static T SerializeDeserialize<T>(T data, ReplicationModel model = null)
        {
            model = model ?? new ReplicationModel();
            var ser = new BinarySerializer(model);
            var bytes = ser.Serialize(data);
            return ser.Deserialize<T>(bytes);
        }
    }
    public static class BinaryGraphUtil
    {
        public struct ClientServer
        {
            public ReplicationModel model;
            public PassThroughChannel channel;
            public ReplicationManager server;
            public ReplicationManager client;
        }
        public static ClientServer MakeClientServer()
        {
            ReplicationModel model = new ReplicationModel();
            PassThroughChannel channel = new PassThroughChannel();
            channel.SetSerializer(new BinaryGraphSerializer(model));
            return new ClientServer()
            {
                model = model,
                channel = channel,
                server = new ReplicationManager(channel.PointA, model),
                client = new ReplicationManager(channel.PointB, model)
            };
        }

        public static T SerializeDeserialize<T>(T data, ReplicationModel model = null)
        {
            model = model ?? new ReplicationModel();
            var ser = new BinaryGraphSerializer(model);
            var stream = ser.Serialize(data);
            return ser.Deserialize<T>(stream);
        }
    }
}
