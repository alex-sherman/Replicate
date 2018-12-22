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
            [Replicate]
            public int Property { get; set; }
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
        [Test]
        public void TestSerializeNullObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<PropClass>(stream, null);
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("null", str);
        }
        [Test]
        public void TestDeserializeNullObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("null");
            stream.Position = 0;
            var output = ser.Deserialize<PropClass>(stream);
            Assert.AreEqual(null, output);
        }
        [Test]
        public void TestSerializeNullable0IntObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<int?>(stream, 0);
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("0", str);
        }
        [Test]
        public void TestDeserializeNullable0IntObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("0");
            stream.Position = 0;
            var output = ser.Deserialize<int?>(stream);
            Assert.AreEqual(0, output);
        }
        [Test]
        public void TestSerializeEmptyString()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, "");
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("\"\"", str);
        }
        [Test]
        public void TestDeserializeEmptyString()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("\"\"");
            stream.Position = 0;
            var output = ser.Deserialize<string>(stream);
            Assert.AreEqual("", output);
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
        public void TestSerializeNullableInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<int?>(stream, 1);
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("1", str);
        }
        [Test]
        public void TestDeserializeNullableInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("1");
            stream.Position = 0;
            var output = ser.Deserialize<int?>(stream);
            Assert.AreEqual(1, output);
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
    }
}
