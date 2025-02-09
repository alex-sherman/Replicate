using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;

namespace ReplicateTest {
    public static class BinarySerializerUtil {
        public struct ClientServer {
            public ReplicationModel model;
            public PassThroughChannel channel;
            public ReplicationManager server;
            public ReplicationManager client;
        }
        public static ClientServer MakeClientServer() {
            ReplicationModel model = new ReplicationModel();
            PassThroughChannel channel = new PassThroughChannel();
            channel.SetSerializer(new BinarySerializer(model));
            return new ClientServer() {
                model = model,
                channel = channel,
                server = new ReplicationManager(channel.PointA, model),
                client = new ReplicationManager(channel.PointB, model)
            };
        }

        public static T SerializeDeserialize<T>(T data, ReplicationModel model = null) {
            model = model ?? new ReplicationModel();
            var ser = new BinarySerializer(model);
            var bytes = ser.Serialize(data);
            return ser.Deserialize<T>(bytes);
        }
    }
}
