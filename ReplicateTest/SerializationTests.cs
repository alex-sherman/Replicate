using System;
using Replicate;
using System.Diagnostics;
using Replicate.MetaData;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Replicate.Messages;
using NUnit.Framework;

namespace ReplicateTest
{
    [TestFixture]
    public class SerializationTests
    {
        [ReplicateType]
        public class GenericClass<T>
        {
            public T Value;
            public T Prop { get; set; }
        }
        [ReplicateType]
        public class PropClass
        {
            public int Property { get; set; }
        }
        [ReplicateType]
        public class SubClass : PropClass
        {
            public string Field;
        }
        [ReplicateType]
        public class GenericSubClass<T, V> : GenericClass<T>
        {
            public V OtherValue;
        }
        [Test]
        public void TestProperty()
        {
            var output = Util.SerializeDeserialize(new PropClass() { Property = 3 });
            Assert.AreEqual(3, output.Property);
        }
        [Test]
        public void TestGeneric()
        {
            var output = Util.SerializeDeserialize(new GenericClass<string>() { Value = "herp", Prop = "derp" });
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [Test]
        public void TestList()
        {
            var output = Util.SerializeDeserialize(new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        [Test]
        public void TestDictionary()
        {
            var output = Util.SerializeDeserialize(new Dictionary<string, PropClass>()
            {
                ["faff"] = new PropClass() { Property = 3 },
                ["herp"] = new PropClass() { Property = 4 }
            });
            Assert.AreEqual(3, output["faff"].Property);
            Assert.AreEqual(4, output["herp"].Property);
        }
        [Test]
        public void TestNullObject()
        {
            var output = Util.SerializeDeserialize<PropClass>(null);
            Assert.IsNull(output);
        }
        [Test]
        public void TestInitMessage()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new InitMessage()
            {
                id = new ReplicatedID() { ObjectID = 0, Creator = 1 },
                typeID = new TypeID()
                {
                    id = 12
                }
            });
            stream.Seek(0, SeekOrigin.Begin);
            var output = ser.Deserialize<InitMessage>(stream);
            Assert.AreEqual(12, output.typeID.id);
        }
        [Test]
        public void TestInheritedType()
        {
            var value = Util.SerializeDeserialize(new SubClass()
            {
                Field = "test",
                Property = 5
            });
            Assert.AreEqual(value.Field, "test");
            Assert.AreEqual(value.Property, 5);
        }
        [Test]
        public void TestNestedGeneric()
        {
            var value = Util.SerializeDeserialize(new GenericSubClass<GenericClass<int>, GenericClass<string>>()
            {
                Value = new GenericClass<int>() { Value = 1 },
                OtherValue = new GenericClass<string>() { Value = "faff" }
            });
            Assert.AreEqual(value.Value.Value, 1);
            Assert.AreEqual(value.OtherValue.Value, "faff");
        }
    }
}
