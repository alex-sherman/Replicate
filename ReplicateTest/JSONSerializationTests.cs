using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestClass]
    public class JSONSerializationTests
    {
        #region Types
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
        #endregion

        [TestMethod]
        public void TestSerializeProperty()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new PropClass() { Property = 3 });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("{\"Property\": 3}", str);
        }
        [TestMethod]
        public void TestSerializeList()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new[] { 1, 2, 3, 4 });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("[1, 2, 3, 4]", str);
        }
        [TestMethod]
        public void TestDeserializeList()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("[1, 2, 3, 4]");
            stream.Position = 0;
            var output = ser.Deserialize<int[]>(stream);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, output);
        }
        [TestMethod]
        public void TestSerializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize(stream, new GenericClass<string>() { Value = "herp", Prop = "derp" });
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("{\"Value\": \"herp\", \"Prop\": \"derp\"}", str);
        }
        [TestMethod]
        public void TestDeserializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString("{\"Value\": \"herp\", \"Prop\": \"derp\"}");
            stream.Position = 0;
            var output = ser.Deserialize< GenericClass<string>>(stream);
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [TestMethod]
        public void TestSerializeNullObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            ser.Serialize<PropClass>(stream, null);
            stream.Position = 0;
            var str = stream.ReadAllString();
            Assert.AreEqual("null", str);
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
    }
}
