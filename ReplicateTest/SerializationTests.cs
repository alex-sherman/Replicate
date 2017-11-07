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
        [Replicate(MarshalMethod.Value)]
        public class SubClass : PropClass
        {
            [Replicate]
            public string Field;
        }
        [Replicate(MarshalMethod.Value)]
        public class GenericSubClass<T, V> : GenericClass<T>
        {
            [Replicate]
            public V OtherValue;
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
        T SerializeDeserialize<T>(T data)
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, data);
            stream.Seek(0, SeekOrigin.Begin);
            return ser.Deserialize<T>(stream);
        }
        [TestMethod]
        public void TestProperty()
        {
            var output = SerializeDeserialize(new PropClass() { Property = 3 });
            Assert.AreEqual(3, output.Property);
        }
        [TestMethod]
        public void TestGeneric()
        {
            var output = SerializeDeserialize(new GenericClass<string>() { Value = "herp" });
            Assert.AreEqual("herp", output.Value);
        }
        [TestMethod]
        public void TestList()
        {
            var output = SerializeDeserialize(new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        [TestMethod]
        public void TestDictionary()
        {
            var output = SerializeDeserialize(new Dictionary<string, PropClass>()
            {
                ["faff"] = new PropClass() { Property = 3 },
                ["herp"] = new PropClass() { Property = 4 }
            });
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
        [TestMethod]
        public void TestInheritedType()
        {
            var value = SerializeDeserialize(new SubClass()
            {
                Field = "test",
                Property = 5
            });
            Assert.AreEqual(value.Field, "test");
            Assert.AreEqual(value.Property, 5);
        }
        [TestMethod]
        public void TestNestedGeneric()
        {
            var value = SerializeDeserialize(new GenericSubClass<GenericClass<int>, GenericClass<string>>()
            {
                Value = new GenericClass<int>() { Value = 1 },
                OtherValue = new GenericClass<string>() { Value = "faff" }
            });
            Assert.AreEqual(value.Value.Value, 1);
            Assert.AreEqual(value.OtherValue.Value, "faff");
        }
    }
}
