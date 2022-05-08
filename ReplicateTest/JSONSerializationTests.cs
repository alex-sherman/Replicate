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
    public class JSONSerializationTests
    {
        #region Types
        [ReplicateType]
        public enum JSONEnum
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
        [ReplicateType]
        public class BlobType
        {
            public Blob Blob;
        }
        [ReplicateType]
        public struct KeyType
        {
            public string Name;
        }
        [ReplicateType]
        public struct NonSerializedField
        {
            [NonSerialized]
            public string Skipped;
            public string NotSkipped;
        }
        #endregion

        [Test]
        public void SerializeProperty()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Property\": 3}", ser.SerializeString(new PropClass() { Property = 3 }));
        }
        [Test]
        public void SerializeArray()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeArray()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<int[]>("[1, 2, 3, 4]"));
        }
        [Test]
        public void DeserializeList()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<List<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeCollection()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<ICollection<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeCollection()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<ICollection<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeIEnumerable()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<IEnumerable<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeIEnumerable()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<IEnumerable<int>>("[1, 2, 3, 4]").ToArray());
        }


        [Test]
        public void SerializeHashset()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new HashSet<int>(new[] { 1, 2, 3, 4 })));
        }
        [Test]
        public void DeserializeHashset()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<HashSet<int>>("[1, 2, 3, 4]").ToArray());
        }
        [Test]
        public void SerializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Value\": \"herp\", \"Prop\": \"derp\"}",
                ser.SerializeString(new GenericClass<string>() { Value = "herp", Prop = "derp" }));
        }
        [Test]
        public void DeserializeGeneric()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<GenericClass<string>>("{\"Value\": \"herp\", \"Prop\": \"derp\"}");
            Assert.AreEqual("herp", output.Value);
            Assert.AreEqual("derp", output.Prop);
        }
        [TestCase(null, typeof(string), "null")]
        [TestCase(0, typeof(int?), "0")]
        [TestCase(1, typeof(int?), "1")]
        [TestCase((ushort)1, typeof(ushort), "1")]
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
        [TestCase(76561198000857376, typeof(long), "76561198000857376")]
        [TestCase(JSONEnum.One, typeof(JSONEnum), "1")]
        [TestCase("\"", typeof(string), "\"\\\"\"")]
        public void SerializeDeserialize(object obj, Type type, string serialized)
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var str = ser.SerializeString(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void FieldEmptyString()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var str = ser.SerializeString(new SubClass() { Field = "" });
            CollectionAssert.AreEqual("{\"Field\": \"\", \"Property\": 0}", str);
            var output = ser.Deserialize<SubClass>("{\"Field\": \"\"}");
            Assert.AreEqual("", output.Field);
        }
        [Test]
        public void Dictionary()
        {
            var serialized = "{\"value\": \"herp\", \"prop\": \"derp\"}";
            var obj = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } };
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
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
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var str = ser.SerializeString(obj);
            Assert.AreEqual(serialized, str);
            var output = ser.Deserialize(obj.GetType(), str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void DictionaryProperty()
        {
            var serialized = "{\"Dict\": {\"value\": \"herp\", \"prop\": \"derp\"}}";
            var obj = new ObjectWithDictField() { Dict = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } } };
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var str = ser.SerializeString(obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize<ObjectWithDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
        }
        [Test]
        public void DictionaryWithSurrogateKey()
        {
            Dictionary<KeyType, string> dict = new Dictionary<KeyType, string>
            {
                { new KeyType() { Name = "a" }, "derp" },
                { new KeyType() { Name = "b" }, "faff" },
            };
            var model = new ReplicationModel() { DictionaryAsObject = true };
            model[typeof(KeyType)].SetSurrogate(new Surrogate(typeof(string),
                (_, __) => (s, v) => ((KeyType)v).Name,
                (_, __) => (s, v) => new KeyType() { Name = (string)v }
            ));
            var ser = new JSONSerializer(model);
            var str = ser.SerializeString(dict);
            var expected = "{\"a\": \"derp\", \"b\": \"faff\"}";
            CollectionAssert.AreEqual(expected, str);
            var output = ser.Deserialize<Dictionary<KeyType, string>>(str);
        }
        [Test]
        public void NullableNullInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.SerializeString<int?>(null);
            Assert.AreEqual("null", str);
        }
        [Test]
        public void DeserializeNullableNullInt()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<int?>("null");
            Assert.AreEqual(null, output);
        }
        [Test]
        public void DeserializeObjectWithEmptyArray()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ArrayField\": [], \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeObjectWithEmptyObject()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ObjectField\": {}, \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeSurrogate()
        {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            model[typeof(ObjectWithDictField)].SetSurrogate(typeof(ObjectWithDictFieldSurrogate));
            var ser = new JSONSerializer(model);
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
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField());
            Assert.AreEqual("{}", output);
        }
        [Test]
        public void IncludesNotNullFields()
        {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField() { NullableValue = 1 });
            Assert.AreEqual("{\"NullableValue\": 1}", output);
        }
        [Test]
        public void Blob()
        {
            var str = "{\"this\": \"is a blob\"}";
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            stream.WriteString(str);
            stream.Position = 0;
            var blob = new BlobType() { Blob = new Blob(stream) };
            var output = ser.SerializeString(blob);
            Assert.AreEqual("{\"Blob\": {\"this\": \"is a blob\"}}", output);
            var deser = ser.Deserialize<BlobType>(output);
        }
        
        [Test]
        public void SkipsNonSerializedFields()
        {
            var model = new ReplicationModel();
            var accessor = model.GetTypeAccessor(typeof(NonSerializedField));
            Assert.IsNull(accessor.SerializedMembers["Skipped"]);
            var ser = new JSONSerializer(model);
            var output = ser.SerializeString(new NonSerializedField() { Skipped = "derp", NotSkipped = "herp" });
            Assert.AreEqual("{\"NotSkipped\": \"herp\"}", output);
        }
    }
}
