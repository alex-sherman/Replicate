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
    public static class Util
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
            var stream = new MemoryStream();
            ser.Serialize(stream, data);
            stream.Seek(0, SeekOrigin.Begin);
            return ser.Deserialize<T>(stream);
        }
    }
}
