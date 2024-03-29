﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaData.Policy;
using Replicate.Serialization;

namespace ReplicateTest {
    [TestFixture]
    public class JSONSerializationTests {
        #region Types
        [ReplicateType]
        public enum JSONEnum {
            One = 1,
            Two = 2,
        }
        [ReplicateType]
        public class GenericClass<T> {
            [Replicate]
            public T Value;
            [Replicate]
            public T Prop { get; set; }
        }
        [ReplicateType]
        public class PropClass {
            public int Property { get; set; }
        }
        [ReplicateType]
        public class ObjectWithArrayField {
            public ObjectWithArrayField ObjectField;
            public double[] ArrayField;
            public int? NullableValue;
        }
        [ReplicateType]
        public class ObjectWithNullableField {
            [SkipNull]
            public int? NullableValue;
        }
        [ReplicateType]
        public class ObjectWithSkipEmptyField {
            [SkipEmpty]
            public List<int> List = new List<int>();
        }
        [ReplicateType]
        public class SubClass : PropClass {
            [Replicate]
            public string Field;
        }
        [ReplicateType]
        public class GenericSubClass<T, V> : GenericClass<T> {
            [Replicate]
            public V OtherValue;
        }
        [ReplicateType]
        public class ObjectWithDictField {
            public Dictionary<string, string> Dict;
        }
        [ReplicateType]
        public class ObjectWithDictFieldSurrogate {
            public Dictionary<string, string> Dict;
            public static implicit operator ObjectWithDictField(ObjectWithDictFieldSurrogate @this) {
                return new ObjectWithDictField() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key.Replace("faff", ""), kvp => kvp.Value) };
            }
            public static implicit operator ObjectWithDictFieldSurrogate(ObjectWithDictField @this) {
                return new ObjectWithDictFieldSurrogate() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key + "faff", kvp => kvp.Value) };
            }
        }
        [ReplicateType]
        public class BlobType {
            public Blob Blob;
        }
        [ReplicateType]
        public struct KeyType {
            public string Name;
        }
        [ReplicateType]
        public struct NonSerializedField {
            [NonSerialized]
            public string Skipped;
            public string NotSkipped;
        }
        [ReplicateType]
        public class RecursiveType {
            public RecursiveType Child;
        }
        #endregion

        [Test]
        public void SerializeProperty() {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Property\": 3}", ser.SerializeString(new PropClass() { Property = 3 }));
        }
        [Test]
        public void SerializeArray() {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeArray() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<int[]>("[1, 2, 3, 4]"));
        }
        [Test]
        public void DeserializeList() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<List<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeCollection() {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<ICollection<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeCollection() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ser.Deserialize<ICollection<int>>("[1, 2, 3, 4]"));
        }
        [Test]
        public void SerializeIEnumerable() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString<IEnumerable<int>>(new[] { 1, 2, 3, 4 }));
        }
        [Test]
        public void DeserializeIEnumerable() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<IEnumerable<int>>("[1, 2, 3, 4]").ToArray());
        }


        [Test]
        public void SerializeHashset() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual("[1, 2, 3, 4]", ser.SerializeString(new HashSet<int>(new[] { 1, 2, 3, 4 })));
        }
        [Test]
        public void DeserializeHashset() {
            var ser = new JSONSerializer(new ReplicationModel());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                ser.Deserialize<HashSet<int>>("[1, 2, 3, 4]").ToArray());
        }
        [Test]
        public void SerializeGeneric() {
            var ser = new JSONSerializer(new ReplicationModel());
            Assert.AreEqual("{\"Value\": \"herp\", \"Prop\": \"derp\"}",
                ser.SerializeString(new GenericClass<string>() { Value = "herp", Prop = "derp" }));
        }
        [Test]
        public void DeserializeGeneric() {
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
        public void SerializeDeserialize(object obj, Type type, string serialized) {
            var ser = new JSONSerializer(new ReplicationModel());
            var str = ser.SerializeString(type, obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize(type, str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void FieldEmptyString() {
            var ser = new JSONSerializer(new ReplicationModel());
            var str = ser.SerializeString(new SubClass() { Field = "" });
            CollectionAssert.AreEqual("{\"Field\": \"\", \"Property\": 0}", str);
            var output = ser.Deserialize<SubClass>("{\"Field\": \"\"}");
            Assert.AreEqual("", output.Field);
        }
        [Test]
        public void Dictionary() {
            var serialized = "{\"value\": \"herp\", \"prop\": \"derp\"}";
            var obj = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } };
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var str = ser.SerializeString(obj);
            Assert.AreEqual(serialized, str);
            var output = ser.Deserialize(obj.GetType(), str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void DictionaryNonStringKey() {
            var serialized = "[{\"Key\": 0, \"Value\": \"herp\"}, {\"Key\": 1, \"Value\": \"derp\"}]";
            var obj = new Dictionary<int, string>() { { 0, "herp" }, { 1, "derp" } };
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var str = ser.SerializeString(obj);
            Assert.AreEqual(serialized, str);
            var output = ser.Deserialize(obj.GetType(), str);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void DictionaryProperty() {
            var serialized = "{\"Dict\": {\"value\": \"herp\", \"prop\": \"derp\"}}";
            var obj = new ObjectWithDictField() { Dict = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } } };
            var ser = new JSONSerializer(new ReplicationModel() { DictionaryAsObject = true });
            var str = ser.SerializeString(obj);
            CollectionAssert.AreEqual(serialized, str);
            var output = ser.Deserialize<ObjectWithDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
        }
        [Test]
        public void DictionaryWithSurrogateKey() {
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
        public void NullableNullInt() {
            var ser = new JSONSerializer(new ReplicationModel());
            var stream = new MemoryStream();
            var str = ser.SerializeString<int?>(null);
            Assert.AreEqual("null", str);
        }
        [Test]
        public void DeserializeNullableNullInt() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<int?>("null");
            Assert.AreEqual(null, output);
        }
        [Test]
        public void DeserializeObjectWithEmptyArray() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ArrayField\": [], \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeObjectWithEmptyObject() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ObjectField\": {}, \"NullableValue\": 1}");
            Assert.AreEqual(1, output.NullableValue);
        }
        [Test]
        public void DeserializeSurrogate() {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            model[typeof(ObjectWithDictField)].SetSurrogate(typeof(ObjectWithDictFieldSurrogate));
            var ser = new JSONSerializer(model);
            var obj = new ObjectWithDictField() {
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
        public void HandlesExtraObjectFields() {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<ObjectWithArrayField>("{\"ExtraField\": \"extra value\"}");
            Assert.AreEqual(null, output.NullableValue);
            Assert.AreEqual(null, output.ArrayField);
            Assert.AreEqual(null, output.ObjectField);
        }
        [Test]
        public void StrictObjectMistmatchException() {
            var ser = new JSONSerializer(new ReplicationModel(), new JSONSerializer.Configuration() { Strict = true });
            Assert.Throws(typeof(SerializationError), () => ser.Deserialize<ObjectWithArrayField>("3"));
        }
        [Test]
        public void NonStrictObjectMismatchNull() {
            var ser = new JSONSerializer(new ReplicationModel(), new JSONSerializer.Configuration() { Strict = false });
            Assert.AreEqual(null, ser.Deserialize<ObjectWithArrayField>("3"));
        }
        [Test]
        public void StrictArrayMistmatchException() {
            var ser = new JSONSerializer(new ReplicationModel(), new JSONSerializer.Configuration() { Strict = true });
            Assert.Throws(typeof(SerializationError), () => ser.Deserialize<List<int>>("3"));
        }
        [Test]
        public void NonStrictArrayMismatchNull() {
            var ser = new JSONSerializer(new ReplicationModel(), new JSONSerializer.Configuration() { Strict = false });
            Assert.AreEqual(null, ser.Deserialize<List<int>>("3"));
        }
        [Test]
        public void SkipsNullFields() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField());
            Assert.AreEqual("{}", output);
        }
        [Test]
        public void SkipsEmptyFields() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithSkipEmptyField());
            Assert.AreEqual("{}", output);
            output = ser.SerializeString(new ObjectWithSkipEmptyField() { List = { 1 } });
            Assert.AreEqual("{\"List\": [1]}", output);
            output = ser.SerializeString(new ObjectWithSkipEmptyField() { List = null });
            Assert.AreEqual("{\"List\": null}", output);
        }
        [Test]
        public void IncludesNotNullFields() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new ObjectWithNullableField() { NullableValue = 1 });
            Assert.AreEqual("{\"NullableValue\": 1}", output);
        }
        [Test]
        public void BlobTest() {
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
        public void BlobSerializesTwice() {
            var ser = new JSONSerializer(new ReplicationModel());
            var blob = Blob.FromString("{\"this\": \"is a blob\"}");
            var output = ser.SerializeString(blob);
            Assert.AreEqual("{\"this\": \"is a blob\"}", output);
            output = ser.SerializeString(blob);
            Assert.AreEqual("{\"this\": \"is a blob\"}", output);
            output = ser.SerializeString(blob);
            Assert.AreEqual("{\"this\": \"is a blob\"}", output);
            var deser = ser.Deserialize<Blob>(output);
        }

        [Test]
        public void SkipsNonSerializedFields() {
            var model = new ReplicationModel();
            var accessor = model.GetTypeAccessor(typeof(NonSerializedField));
            Assert.IsNull(accessor.SerializedMembers["Skipped"]);
            var ser = new JSONSerializer(model);
            var output = ser.SerializeString(new NonSerializedField() { Skipped = "derp", NotSkipped = "herp" });
            Assert.AreEqual("{\"NotSkipped\": \"herp\"}", output);
        }

        [Test]
        public void SkipsRecursiveChildren() {
            var model = new ReplicationModel();
            var ser = new JSONSerializer(model);
            var recursiveObject = new RecursiveType() { Child = new RecursiveType() };
            recursiveObject.Child.Child = recursiveObject;
            var output = ser.SerializeString(recursiveObject);
            Assert.AreEqual("{\"Child\": {\"Child\": null}}", output);
        }
        [ReplicateType]
        public class Parent { public Parent() { } public Parent(float herp) { Herp = herp; } public float Herp { get; private set; } }
        [ReplicateType]
        public class ClassWithParent { public Parent Parent; }
        [ReplicateType]
        public class ChildA : Parent { public ChildA() { } public ChildA(float herp) : base(herp) { } public int Field = 3; }
        [ReplicateType]
        public class ChildB : Parent { public string Property { get; set; } = "herp"; }
        [Test]
        public void ParentPolymorphism() {
            var model = new ReplicationModel();
            var ser = new JSONSerializer(model);
            model[typeof(Parent)].SetSurrogate(TypedBlob.MakeSurrogate(typeof(Parent)));
            Parent parentObjectA = new ChildA();
            var outputA = ser.SerializeString(parentObjectA);
            var deserParentA = ser.Deserialize<Parent>(outputA);
            Assert.AreEqual(deserParentA.GetType(), typeof(ChildA));
            Assert.AreEqual((deserParentA as ChildA).Field, 3);

            Parent parentObjectB = new ChildB();
            var outputB = ser.SerializeString(parentObjectB);
            var deserParentB = ser.Deserialize<Parent>(outputB);
            Assert.AreEqual(deserParentB.GetType(), typeof(ChildB));
            Assert.AreEqual((deserParentB as ChildB).Property, "herp");

            Assert.Throws(typeof(InvalidOperationException), () => {
                ser.Deserialize<Parent>(ser.SerializeString(new Parent()));
            });
        }
        [Test]
        public void ParentPolymorphismFromMember() {
            var model = new ReplicationModel();
            var ser = new JSONSerializer(model);
            model[typeof(ClassWithParent)][nameof(ClassWithParent.Parent)]
                .SetSurrogate(TypedBlob.MakeSurrogate(typeof(Parent), throwIfSameType: false));
            var parentObjectA = new ClassWithParent() { Parent = new ChildA() };
            var outputA = ser.SerializeString(parentObjectA);
            var deserParentA = ser.Deserialize<ClassWithParent>(outputA);
            Assert.AreEqual(deserParentA.Parent.GetType(), typeof(ChildA));
            Assert.AreEqual((deserParentA.Parent as ChildA).Field, 3);

            var parentObjectB = new ClassWithParent() { Parent = new ChildB() };
            var outputB = ser.SerializeString(parentObjectB);
            var deserParentB = ser.Deserialize<ClassWithParent>(outputB);
            Assert.AreEqual(deserParentB.Parent.GetType(), typeof(ChildB));
            Assert.AreEqual((deserParentB.Parent as ChildB).Property, "herp");

            var parentObject = new ClassWithParent() { Parent = new Parent() };
            var output = ser.SerializeString(parentObject);
            var deserParent = ser.Deserialize<ClassWithParent>(output);
            Assert.AreEqual(deserParent.Parent.GetType(), typeof(Parent));
        }
        [Test]
        public void ParentPropertyWithPrivateAccessor() {
            var model = new ReplicationModel(false);
            model.Add(typeof(Parent));
            model.Add(typeof(ChildA));
            var ser = new JSONSerializer(model, new JSONSerializer.Configuration() { Strict = false });
            var deser = ser.Deserialize<Parent>(ser.SerializeString(new Parent(1)));
            Assert.AreEqual(deser.Herp, 1);
            deser = ser.Deserialize<Parent>(ser.SerializeString(new ChildA(1)));
            Assert.AreEqual(deser.Herp, 1);
            deser = ser.Deserialize<ChildA>(ser.SerializeString(new ChildA(1)));
            Assert.AreEqual(deser.Herp, 1);
        }
        [Test]
        public void TrailingCommaInArray() {
            var model = new ReplicationModel(false);
            var ser = new JSONSerializer(model, new JSONSerializer.Configuration() { Strict = false });
            CollectionAssert.AreEqual(new[] { 1 }, ser.Deserialize<List<int>>("[1,]"));
        }
        [ReplicateType]
        public struct ParentStruct {
            public ChildStruct Child;
        }
        [ReplicateType]
        public struct ChildStruct {
            public int Num;
        }
        [ReplicateType]
        public struct ChildWithSurrogate {
            public int Num;
        }
        [ReplicateType]
        public enum Enum {
            A, B, C
        }
        [Test]
        public void NestedStructType() {
            var model = new ReplicationModel(false) { DictionaryAsObject = true };
            model.Add(typeof(ChildStruct));
            model.Add(typeof(ChildWithSurrogate)).SetSurrogate(Surrogate.Simple<ChildWithSurrogate, int>(c => c.Num, n => new ChildWithSurrogate() { Num = n }));
            model.Add(typeof(ParentStruct));
            model.Add(typeof(Enum));
            var ser = new JSONSerializer(model);
            var str = ser.SerializeString(new ParentStruct() { Child = new ChildStruct() { Num = 0xFAFF } });
            Assert.AreEqual(str, "{\"Child\": {\"Num\": 64255}}");
            str = ser.SerializeString(new Dictionary<string, KeyValuePair<string, ChildStruct>> { { "a", new KeyValuePair<string, ChildStruct>("b", new ChildStruct() { Num = 0xFAFF }) } });
            Assert.AreEqual(str, "{\"a\": {\"Key\": \"b\", \"Value\": {\"Num\": 64255}}}");
            str = ser.SerializeString(new Dictionary<string, KeyValuePair<string, ChildWithSurrogate>> { { "a", new KeyValuePair<string, ChildWithSurrogate>("b", new ChildWithSurrogate() { Num = 0xFAFF }) } });
            Assert.AreEqual(str, "{\"a\": {\"Key\": \"b\", \"Value\": 64255}}");
        }
    }
}
