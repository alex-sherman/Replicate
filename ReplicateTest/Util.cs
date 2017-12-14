﻿using Replicate;
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
        public static ClientServer MakeClientServer(Serializer serializer = null)
        {
            ReplicationModel model = new ReplicationModel();
            serializer = serializer ?? new BinarySerializer(model);
            PassThroughChannel channel = new PassThroughChannel();
            return new ClientServer()
            {
                model = model,
                channel = channel,
                client = new ReplicationManager(serializer, model) { ID = 1 }.RegisterClient(0, channel.CreateEndpoint()),
                server = new ReplicationManager(serializer, model) { ID = 0 }.RegisterClient(1, channel.CreateEndpoint())
            };
        }

        public static T SerializeDeserialize<T>(T data, ReplicationModel model = null)
        {
            model = model ?? new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, data);
            stream.Seek(0, SeekOrigin.Begin);
            return ser.Deserialize<T>(stream);
        }
    }
}
