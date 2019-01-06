using System;
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
            var ser = new JSONGraphSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Property\": 3}", ser.Serialize(new PropClass() { Property = 3 }));
        }
        [Test]
        public void TestSerializeList()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.Serialize(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void TestDeserializeList()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<int[]>("[1, 2, 3, 4]"));
        }
        [Test]
        public void TestSerializeIEnumerable()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.Serialize<IEnumerable<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void TestDeserializeIEnumerable()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<IEnumerable<int>>("[1, 2, 3, 4]").ToArray());
        }


        [Test]
        public void TestSerializeHashset()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.Serialize(new HashSet<int>(new[] { 1, 2, 3, 4 })));
        }
        [Test]
        public void TestDeserializeHashset()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<HashSet<int>>("[1, 2, 3, 4]").ToArray());
        }
        [Test]
        public void TestSerializeGeneric()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Value\": \"herp\", \"Prop\": \"derp\"}",
                ser.Serialize(new GenericClass<string>() { Value = "herp", Prop = "derp" }));
        }
        [Test]
        public void TestDeserializeGeneric()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<GenericClass<string>>("{\"Value\": \"herp\", \"Prop\": \"derp\"}");
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
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void TestFieldEmptyString()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
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
            var ser = new JSONGraphSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var stream = new MemoryStream();
            var str = ser.Serialize(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void TestNullableNullInt()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.Serialize<int?>(null);
            Assert.AreEqual("null", str);
        }
        [Test]
        public void TestDeserializeNullableNullInt()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<int?>("null");
            Assert.AreEqual(null, output);
        }
        [Test]
        public void TestDeserializeObjectWithEmptyArray()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ArrayField\": [], \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void TestDeserializeObjectWithEmptyObject()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ObjectField\": {}, \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
    }
}
