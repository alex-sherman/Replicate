using System;
using Replicate;
using System.Diagnostics;
using Replicate.MetaData;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Replicate.Messages;
using NUnit.Framework;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestFixture]
    public class BinarySerializationTests
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
            var output = BinarySerializerUtil.SerializeDeserialize(new PropClass() { Property = 3 });
            Assert.AreEqual(3, output.Property);
        }
        [Test]
        public void TestGeneric()
        {
            var output = BinarySerializerUtil.SerializeDeserialize(new GenericClass<string>() { Value = "herp", Prop = "derp" });
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [Test]
        public void TestList()
        {
            var output = BinarySerializerUtil.SerializeDeserialize(new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        // TODO: Fix
        //[Test]
        //public void TestSerializeRepBackedNode()
        //{
        //    var model = new ReplicationModel();
        //    var output = SerializeDeserialize((IRepNode)new RepBackedNode(new PropClass() { Property = 3 }, model: model), model);
        //    Assert.IsInstanceOf<PropClass>(output.RawValue);
        //    Assert.AreEqual(3, ((PropClass)output.RawValue).Property);
        //}
        [TestCase(null, typeof(string), "null")]
        [TestCase(0, typeof(int?), "0")]
        [TestCase(1, typeof(int?), "1")]
        [TestCase(1, typeof(short), "1")]
        [TestCase(1, typeof(ushort), "1")]
        [TestCase(1, typeof(long), "1")]
        [TestCase(1, typeof(ulong), "1")]
        [TestCase(null, typeof(int?), "null")]
        [TestCase(0.5, typeof(float), "0.5")]
        [TestCase(0, typeof(float?), "0")]
        [TestCase(1, typeof(float?), "1")]
        [TestCase(null, typeof(float?), "null")]
        [TestCase("", typeof(string), "\"\"")]
        [TestCase("😈", typeof(string), "\"😈\"")]
        [TestCase(new double[] { }, typeof(double[]), "[]")]
        [TestCase(null, typeof(double[]), "null")]
        [TestCase(true, typeof(bool), "true")]
        [TestCase(false, typeof(bool), "false")]
        //[TestCase(JSONEnum.One, typeof(JSONEnum), "1")]
        public void SerializeDeserialize(object obj, Type type, string serialized)
        {
            var ser = new BinarySerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize(type, obj);
            //CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void TestDictionary()
        {
            var output = BinarySerializerUtil.SerializeDeserialize(new Dictionary<string, PropClass>()
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
            var output = BinarySerializerUtil.SerializeDeserialize<PropClass>(null);
            Assert.IsNull(output);
        }
        [Test]
        public void TestInitMessage()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = ser.Serialize(new InitMessage()
            {
                id = new ReplicateId() { ObjectID = 0, Creator = 1 },
                typeID = new TypeID()
                {
                    id = 12
                }
            });
            var output = ser.Deserialize<InitMessage>(stream);
            Assert.AreEqual(12, output.typeID.id);
        }
        [Test]
        public void TestInheritedType()
        {
            var value = BinarySerializerUtil.SerializeDeserialize(new SubClass()
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
            var value = BinarySerializerUtil.SerializeDeserialize(new GenericSubClass<GenericClass<int>, GenericClass<string>>()
            {
                Value = new GenericClass<int>() { Value = 1 },
                OtherValue = new GenericClass<string>() { Value = "faff" }
            });
            Assert.AreEqual(value.Value.Value, 1);
            Assert.AreEqual(value.OtherValue.Value, "faff");
        }
    }
}
