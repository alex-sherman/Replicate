using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using System.Diagnostics;
using Replicate.MetaData;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Replicate.Messages;

namespace ReplicateTest
{
    [TestClass]
    public class SerializationTests
    {
        [Replicate]
        public struct SimpleMessage
        {
            [Replicate]
            public float time;
            [Replicate]
            public string faff;
        }
        [Replicate]
        public class GenericClass<T>
        {
            [Replicate]
            public T Value;
        }
        [Replicate(MarshalMethod.Value)]
        public class PropClass
        {
            [Replicate]
            public int Property { get; set; }
        }
        [TestMethod]
        public void TestSendRecv()
        {
            var testMessage = new SimpleMessage()
            {
                time = 10,
                faff = "FAFF"
            };
            var cs = Util.MakeClientServer();
            bool called = false;
            cs.client.RegisterHandler<SimpleMessage>(0, (message) =>
            {
                called = true;
                Assert.AreEqual(message, testMessage);
            });
            cs.server.Send(0, testMessage);
            cs.client.Receive().Wait();
            Assert.IsTrue(called);
        }
        [TestMethod]
        public void TestProperty()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new PropClass() { Property = 3 });
            stream.Seek(0, SeekOrigin.Begin);
            object output = ser.Deserialize(null, stream, typeof(PropClass), null, model[typeof(PropClass)]);
            Assert.AreEqual(3, (output as PropClass).Property);
        }
        [TestMethod]
        public void TestGeneric()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new GenericClass<string>() { Value = "herp" });
            stream.Seek(0, SeekOrigin.Begin);
            object output = ser.Deserialize(null, stream, typeof(GenericClass<string>), null, model[typeof(GenericClass<string>)]);
            Assert.AreEqual("herp", (output as GenericClass<string>).Value);
        }
        [TestMethod]
        public void TestList()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            stream.Seek(0, SeekOrigin.Begin);
            var output = (List<PropClass>)ser.Deserialize(null, stream, typeof(List<PropClass>), null);
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        [TestMethod]
        public void TestDictionary()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new Dictionary<string, PropClass>()
            {
                ["faff"] = new PropClass() { Property = 3 },
                ["herp"] = new PropClass() { Property = 4 }
            });
            stream.Seek(0, SeekOrigin.Begin);
            var output = (Dictionary<string, PropClass>)ser.Deserialize(null, stream, typeof(Dictionary<string, PropClass>), null);
            Assert.AreEqual(3, output["faff"].Property);
            Assert.AreEqual(4, output["herp"].Property);
        }
        [TestMethod]
        public void TestInitMessage()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new InitMessage()
            {
                id = new ReplicatedID() { objectId = 0, owner = 1 },
                typeID = new TypeID()
                {
                    id = 12
                }
            });
            stream.Seek(0, SeekOrigin.Begin);
            var output = (InitMessage)ser.Deserialize(null, stream, typeof(InitMessage), null);
            Assert.AreEqual(12, output.typeID.id);
        }
    }
}
