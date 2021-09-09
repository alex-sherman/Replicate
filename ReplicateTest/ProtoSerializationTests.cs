using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaData.Policy;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestFixture]
    public class ProtoSerializationTests
    {
        #region Types
        [ReplicateType]
        public enum ProtoEnum
        {
            One = 1,
            Two = 2,
        }
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
        public class ObjectWithNullableField
        {
            [SkipNull]
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
        [ReplicateType]
        public class ObjectWithDictField
        {
            public Dictionary<string, string> Dict;
        }
        [ReplicateType]
        public class ObjectWithDictFieldSurrogate
        {
            public Dictionary<string, string> Dict;
            public static implicit operator ObjectWithDictField(ObjectWithDictFieldSurrogate @this)
            {
                return new ObjectWithDictField() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key.Replace("faff", ""), kvp => kvp.Value) };
            }
            public static implicit operator ObjectWithDictFieldSurrogate(ObjectWithDictField @this)
            {
                return new ObjectWithDictFieldSurrogate() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key + "faff", kvp => kvp.Value) };
            }
        }
        #endregion

        [Test]
        public void Property()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(new PropClass() { Property = 3 });
            Assert.AreEqual(new byte[] { 0x08, 0x03 }, bytes);
        }
        [Test]
        public void SerializeArray()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeArray()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<int[]>("[1, 2, 3, 4]"));
        }
        [Test]
        public void DeserializeList()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<List<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeCollection()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<ICollection<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeCollection()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<ICollection<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeIEnumerable()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<IEnumerable<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeIEnumerable()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<IEnumerable<int>>("[1, 2, 3, 4]").ToArray());
        }


        [Test]
        public void SerializeHashset()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new HashSet<int>(new[] { 1, 2, 3, 4 })));
        }
        [Test]
        public void DeserializeHashset()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<HashSet<int>>("[1, 2, 3, 4]").ToArray());
        }
        [Test]
        public void SerializeGeneric()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Value\": \"herp\", \"Prop\": \"derp\"}",
                ser.SerializeString(new GenericClass<string>() { Value = "herp", Prop = "derp" }));
        }
        [Test]
        public void DeserializeGeneric()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.Deserialize<GenericClass<string>>("{\"Value\": \"herp\", \"Prop\": \"derp\"}");
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        //[TestCase(null, typeof(string), "null")]
        [TestCase(0, typeof(int), new byte[] { 0 })]
        [TestCase(1, typeof(int), new byte[] { 1 })]
        [TestCase(300, typeof(int), new byte[] { 0xAC, 0x02 })]
        //[TestCase((ushort)1, typeof(ushort), "1")]
        //[TestCase(null, typeof(int?), "null")]
        //[TestCase(0.5, typeof(float), "0.5")]
        //[TestCase(0, typeof(float?), "0")]
        //[TestCase(1, typeof(float?), "1")]
        //[TestCase(null, typeof(float?), "null")]
        [TestCase("faff", typeof(string), new byte[] { 4, (byte)'f', (byte)'a', (byte)'f', (byte)'f' })]
        //[TestCase("", typeof(string), "\"\"")]
        //[TestCase("😈", typeof(string), "\"😈\"")]
        //[TestCase(new double[] { }, typeof(double[]), "[]")]
        //[TestCase(null, typeof(double[]), "null")]
        //[TestCase(true, typeof(bool), "true")]
        //[TestCase(false, typeof(bool), "false")]
        //[TestCase(ProtoEnum.One, typeof(ProtoEnum), "1")]
        //[TestCase("\"", typeof(string), "\"\\\"\"")]
        public void SerializeDeserialize(object obj, Type type, byte[] serialized)
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(type, obj);
            CollectionAssert.AreEqual(serialized, bytes);
            var output = ser.Deserialize(type, bytes);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void FieldEmptyString()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.SerializeBytes(new SubClass() { Field = "" });
            //CollectionAssert.AreEqual("{\"Field\": \"\", \"Property\": 0}", str);
            var output = ser.Deserialize<SubClass>("{\"Field\": \"\"}");
            Assert.AreEqual("", output.Field);
        }
        [Test]
        public void Dictionary()
        {
            var serialized = "{\"value\": \"herp\", \"prop\": \"derp\"}";
            var obj = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } };
            var ser = new ProtoSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var stream = new MemoryStream();
            var str = ser.SerializeString(obj);
            Assert.AreEqual(serialized, str);
            var output = ser.Deserialize(obj.GetType(), str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void DictionaryNonStringKey()
        {
            var serialized = "[{\"Key\": 0, \"Value\": \"herp\"}, {\"Key\": 1, \"Value\": \"derp\"}]";
            var obj = new Dictionary<int, string>() { { 0, "herp" }, { 1, "derp" } };
            var ser = new ProtoSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var stream = new MemoryStream();
            var str = ser.SerializeString(obj);
            Assert.AreEqual(serialized, str);
            var output = ser.Deserialize(obj.GetType(), str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void DictionaryProperty()
        {
            var serialized = "{\"Dict\": {\"value\": \"herp\", \"prop\": \"derp\"}}";
            var type = typeof(Dictionary<string, string>);
            var obj = new ObjectWithDictField() { Dict = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } } };
            var ser = new ProtoSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var stream = new MemoryStream();
            var str = ser.SerializeString(obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize<ObjectWithDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
        }
        [Test]
        public void NullableNullInt()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.SerializeString<int?>(null);
            Assert.AreEqual("null", str);
        }
        [Test]
        public void DeserializeNullableNullInt()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.Deserialize<int?>("null");
            Assert.AreEqual(null, output);
        }
        [Test]
        public void DeserializeObjectWithEmptyArray()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ArrayField\": [], \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeObjectWithEmptyObject()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ObjectField\": {}, \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeSurrogate()
        {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            model[typeof(ObjectWithDictField)].SetSurrogate(typeof(ObjectWithDictFieldSurrogate));
            var ser = new ProtoSerializer(model);
            var obj = new ObjectWithDictField()
            {
                Dict = new Dictionary<string, string>()
                {
                    {"a", "herp" },
                    {"b", "derp" },
                },
            };
            var serStr = ser.SerializeString(obj);
            Assert.AreEqual("{\"Dict\": {\"afaff\": \"herp\", \"bfaff\": \"derp\"}}", serStr);
            var deser = ser.Deserialize<ObjectWithDictField>(serStr);
            CollectionAssert.AreEqual(obj.Dict, deser.Dict);
        }
        [Test]
        public void HandlesExtraObjectFields()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ExtraField\": \"extra value\"}");
            Assert.AreEqual(null, output.NullableValue);
            Assert.AreEqual(null, output.ArrayField);
            Assert.AreEqual(null, output.ObjectField);
        }
        [Test]
        public void SkipsNullFields()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField());
            Assert.AreEqual("{}", output);
        }
        [Test]
        public void IncludesNotNullFields()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField() { NullableValue = 1 });
            Assert.AreEqual("{\"NullableValue\": 1}", output);
        }
    }
}
