﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestFixture]
    public class JSONSerializationTests
    {
        #region Types
        [ReplicateType]
        public class GenericClass<T>
        {
            [Replicate]
            public T Value;
            [Replicate]
            public T Prop { get; set; }
        }
        [ReplicateType]
        public class PropClass
        {
            public int Property { get; set; }
        }
        [ReplicateType]
        public class ObjectWithArrayField
        {
            public ObjectWithArrayField ObjectField;
            public double[] ArrayField;
            public int? NullableValue;
        }
        [ReplicateType]
        public class SubClass : PropClass
        {
            [Replicate]
            public string Field;
        }
        [ReplicateType]
        public class GenericSubClass<T, V> : GenericClass<T>
        {
            [Replicate]
            public V OtherValue;
        }
        #endregion

        [Test]
        public void TestSerializeProperty()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new PropClass() { Property = 3 });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("{\"Property\": 3}", str);
        }
        [Test]
        public void TestSerializeList()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new[] { 1, 2, 3, 4 });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("[1, 2, 3, 4]", str);
        }
        [Test]
        public void TestDeserializeList()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("[1, 2, 3, 4]");
            stream.Position = 0;
            var output = ser.Deserialize<int[]>(stream);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, output);
        }
        [Test]
        public void TestSerializeIEnumerable()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<IEnumerable<int>>(stream, new[] { 1, 2, 3, 4 });
            stream.Position = 0;
            var str = stream.ReadAllString();
            CollectionAssert.AreEqual("[1, 2, 3, 4]", str);
        }
        [Test]
        public void TestDeserializeIEnumerable()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("[1, 2, 3, 4]");
            stream.Position = 0;
            var output = ser.Deserialize<IEnumerable<int>>(stream);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, output.ToArray());
        }


        [Test]
        public void TestSerializeHashset()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new HashSet<int>(new[] { 1, 2, 3, 4 }));
            stream.Position = 0;
            var str = stream.ReadAllString();
            CollectionAssert.AreEqual("[1, 2, 3, 4]", str);
        }
        [Test]
        public void TestDeserializeHashset()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("[1, 2, 3, 4]");
            stream.Position = 0;
            var output = ser.Deserialize<HashSet<int>>(stream);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, output.ToArray());
        }
        [Test]
        public void TestSerializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel()) { ToLowerFieldNames = true };
            var stream = new MemoryStream();
            ser.Serialize(stream, new GenericClass<string>() { Value = "herp", Prop = "derp" });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("{\"value\": \"herp\", \"prop\": \"derp\"}", str);
        }
        [Test]
        public void TestDeserializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel()) { ToLowerFieldNames = true };
            var stream = new MemoryStream();
            stream.WriteString("{\"value\": \"herp\", \"prop\": \"derp\"}");
            stream.Position = 0;
            var output = ser.Deserialize< GenericClass<string>>(stream);
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [TestCase(null, typeof(object), "null")]
        [TestCase(0, typeof(int?), "0")]
        [TestCase(1, typeof(int?), "1")]
        [TestCase(null, typeof(int?), "null")]
        [TestCase(0.5, typeof(float), "0.5")]
        [TestCase(0, typeof(float?), "0")]
        [TestCase(1, typeof(float?), "1")]
        [TestCase(null, typeof(float?), "null")]
        [TestCase("", typeof(string), "\"\"")]
        [TestCase("😈", typeof(string), "\"😈\"")]
        [TestCase(new double[] { }, typeof(double[]), "[]")]
        [TestCase(true, typeof(bool), "true")]
        [TestCase(false, typeof(bool), "false")]
        public void TestSerializeDeserialize(object obj, Type type, string serialized)
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void TestFieldEmptyString()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize(new SubClass() { Field = "" });
            CollectionAssert.AreEqual("{\"Field\": \"\", \"Property\": 0}", str);
            var output = ser.Deserialize<string, SubClass>("{\"Field\": \"\"}");
            Assert.AreEqual("", output.Field);
        }
        [Test]
        public void TestDictionary()
        {
            var serialized = "{\"value\": \"herp\", \"prop\": \"derp\"}";
            var type = typeof(Dictionary<string, string>);
            var obj = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } };
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void TestNullableNullInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<int?>(stream, null);
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("null", str);
        }
        [Test]
        public void TestDeserializeNullableNullInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("null");
            stream.Position = 0;
            var output = ser.Deserialize<int?>(stream);
            Assert.AreEqual(null, output);
        }
        [Test]
        public void TestDeserializeObjectWithEmptyArray()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("{\"ArrayField\": [], \"NullableValue\": 1}");
            stream.Position = 0;
            var output = ser.Deserialize<ObjectWithArrayField>(stream);
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void TestDeserializeObjectWithEmptyObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("{\"ObjectField\": {}, \"NullableValue\": 1}");
            stream.Position = 0;
            var output = ser.Deserialize<ObjectWithArrayField>(stream);
            Assert.AreEqual(1, output.NullableValue);
        }
    }
}
