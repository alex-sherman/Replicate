﻿using System;
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
        public class GenericClass<T>
        {
            [Replicate]
            public T Value;
            [Replicate]
            public T Prop { get; set; }
        }
        [Replicate]
        public class PropClass
        {
            [Replicate]
            public int Property { get; set; }
        }
        [Replicate]
        public class SubClass : PropClass
        {
            [Replicate]
            public string Field;
        }
        [Replicate]
        public class GenericSubClass<T, V> : GenericClass<T>
        {
            [Replicate]
            public V OtherValue;
        }
        [TestMethod]
        public void TestProperty()
        {
            var output = Util.SerializeDeserialize(new PropClass() { Property = 3 });
            Assert.AreEqual(3, output.Property);
        }
        [TestMethod]
        public void TestGeneric()
        {
            var output = Util.SerializeDeserialize(new GenericClass<string>() { Value = "herp", Prop = "derp" });
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [TestMethod]
        public void TestList()
        {
            var output = Util.SerializeDeserialize(new List<PropClass>() { new PropClass() { Property = 3 }, new PropClass() { Property = 4 } });
            Assert.AreEqual(3, output[0].Property);
            Assert.AreEqual(4, output[1].Property);
        }
        [TestMethod]
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
        [TestMethod]
        public void TestInitMessage()
        {
            var model = new ReplicationModel();
            var ser = new Replicate.Serialization.BinarySerializer(model);
            var stream = new MemoryStream();
            ser.Serialize(stream, new InitMessage()
            {
                id = new ReplicatedID() { objectId = 0, creator = 1 },
                typeID = new TypeID()
                {
                    id = 12
                }
            });
            stream.Seek(0, SeekOrigin.Begin);
            var output = ser.Deserialize<InitMessage>(stream);
            Assert.AreEqual(12, output.typeID.id);
        }
        [TestMethod]
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
        [TestMethod]
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
