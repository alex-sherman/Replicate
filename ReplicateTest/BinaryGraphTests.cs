using System;
using Replicate;
using System.Diagnostics;
using Replicate.MetaData;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Replicate.Messages;
using NUnit.Framework;
using static ReplicateTest.BinarySerializerUtil;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestFixture]
    public class BinaryGraphTests
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
            var output = SerializeDeserialize(new PropClass() { Property = 3 });
            Assert.AreEqual(3, output.Property);
        }
        [Test]
        public void TestGeneric()
        {
            var output = SerializeDeserialize(new GenericClass<string>() { Value = "herp", Prop = "derp" });
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [Test]
        public void TestList()
        {
            var output = SerializeDeserialize(new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        [Test]
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
        [Test]
        public void TestDictionaryAsObject()
        {
            var output = SerializeDeserialize(new Dictionary<string, PropClass>()
            {
                ["faff"] = new PropClass() { Property = 3 },
                ["herp"] = new PropClass() { Property = 4 }
            }, new ReplicationModel() { DictionaryAsObject = true });
            Assert.AreEqual(3, output["faff"].Property);
            Assert.AreEqual(4, output["herp"].Property);
        }
        [Test]
        public void TestNullObject()
        {
            var output = SerializeDeserialize<PropClass>(null);
            Assert.IsNull(output);
        }
        [Test]
        public void TestInitMessage()
        {
            var model = new ReplicationModel();
            model.Add(typeof(InitMessage));
            var ser = new BinarySerializer(model);
            var stream = ser.Serialize(new InitMessage()
            {
                id = new ReplicateId() { ObjectID = 0, Creator = 1 },
                typeID = new TypeId()
                {
                    Id = 12
                }
            });
            var output = ser.Deserialize<InitMessage>(stream);
            Assert.AreEqual(12, output.typeID.Id.Index);
        }
        [Test]
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
        [Test]
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
