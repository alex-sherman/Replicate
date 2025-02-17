using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaData.Policy;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ReplicateTest.SerializerTest;

namespace ReplicateTest {
    [TestFixture]
    public class JSONSerializationTests {
        #region Types
        [ReplicateType]
        public class ReadOnlyClass {
            [Replicate]
            public string Value => "Derp";
        }
        [ReplicateType]
        public class WriteOnlyClass {
            [Replicate]
            public string Value { set { } }
        }
        [ReplicateType]
        public class ObjectWithArrayField {
            [Replicate]
            public ObjectWithArrayField ObjectField;
            [Replicate]
            public double[] ArrayField;
            [Replicate]
            public int? NullableValue;
        }
        [ReplicateType]
        public class ObjectWithNullableField {
            [Replicate]
            [SkipNull]
            public int? NullableValue;
        }
        [ReplicateType]
        public class ObjectWithSkipEmptyField {
            [Replicate]
            [SkipEmpty]
            public List<int> List = new List<int>();
        }
        [ReplicateType]
        public class GenericSubClass<T, V> : GenericClass<T> {
            [Replicate]
            public V OtherValue;
        }
        [ReplicateType]
        public class ObjectWithDictFieldSurrogate {
            [Replicate]
            public Dictionary<string, string> Dict;
            public static implicit operator ObjectWithDictField<string, string>(ObjectWithDictFieldSurrogate @this) {
                return new ObjectWithDictField<string, string>() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key.Replace("faff", ""), kvp => kvp.Value) };
            }
            public static implicit operator ObjectWithDictFieldSurrogate(ObjectWithDictField<string, string> @this) {
                return new ObjectWithDictFieldSurrogate() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key + "faff", kvp => kvp.Value) };
            }
        }
        [ReplicateType]
        public class BlobType {
            [Replicate]
            public Blob Blob;
        }
        [ReplicateType]
        public struct KeyType {
            [Replicate]
            public string Name;
        }
        [ReplicateType(AutoMembers = AutoAdd.All)]
        public struct NonSerializedField {
            [NonSerialized]
            public string Skipped;
            public string NotSkipped;
        }
        [ReplicateType]
        public class RecursiveType {
            [Replicate]
            public RecursiveType Child;
        }
        [ReplicateType(AutoMembers = AutoAdd.None)]
        public class ExplicitMembersType {
            [Replicate(1)]
            public string FirstString { get; set; }
            [Replicate(2)]
            public string SecondString { get; set; }
        }
        #endregion

        [Test]
        public void ReadOnlyProp() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.Deserialize<ReadOnlyClass>("{\"Value\": \"NotDerp\"}");
            Assert.AreEqual("Derp", output.Value);
        }
        [Test]
        public void WriteOnlyProp() {
            var ser = new JSONSerializer(new ReplicationModel());
            var output = ser.SerializeString(new WriteOnlyClass());
            Assert.AreEqual("{}", output);
        }

        public static IEnumerable<Args> SimpleArgs() {
            return new[] {
                Case((string)null, "null"),
                Case((int?)null, "null"),
                Case((float?)null, "null"),
                Case((double[])null, "null"),
                Case(true, "true"),
                Case(false, "false"),
                Case((int?)0, "0"),
                Case((int?)123, "123"),
                Case(1u, "1"),
                Case(1, "1"),
                Case(300u, "300"),
                Case((short)-1, "-1"),
                Case((ushort)0xfaff, "64255"),
                Case(0.5f, "0.5"),
                Case(0.5d, "0.5"),
                Case((float?)0, "0"),
                Case((float?)1, "1"),
                Case("", "\"\""),
                Case("😈", "\"😈\""),
                Case("\"", "\"\\\"\""),
                Case(new double[] { }, "[]"),
                Case(76561198000857376L, "76561198000857376"),
                Case(JSONEnum.One, "1"),
                Case(JSONEnum.Two, "2"),
                Case(new Guid("dcae97d8-0315-4aa8-94fe-92a91e5983bc"), "\"dcae97d8-0315-4aa8-94fe-92a91e5983bc\""),
            };
        }
        [TestCaseSource(nameof(SimpleArgs))]
        public void SimpleSerDes(Args args) {
            args.SerDes(new JSONSerializer(new ReplicationModel()));
        }

        public static IEnumerable<Args> CollectionArgs() {
            return new[] {
                Case(new int[] { 1, 2, 3, 4 }, "[1, 2, 3, 4]"),
                Case(new List<int> { 1, 2, 3, 4 }, "[1, 2, 3, 4]"),
                Case((ICollection<int>)new List<int> { 1, 2, 3, 4 }, "[1, 2, 3, 4]"),
                Case((IEnumerable<int>)new List<int> { 1, 2, 3, 4 }, "[1, 2, 3, 4]"),
                Case(new HashSet<int>(new[] { 1, 2, 3, 4 }), "[1, 2, 3, 4]"),
            };
        }
        [TestCaseSource(nameof(CollectionArgs))]
        public void CollectionSerDes(Args args) {
            args.SerDes(new JSONSerializer(new ReplicationModel()));
        }

        public static IEnumerable<Args> ObjectArgs() {
            return new[] {
                Case(new SubClass()),
                Case(new SubClass() { Field = "derp" }),
                Case(new SubClass() { Property = 2 }),
                Case(new SubClass() { Field = "" }),
                Case(new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } },
                    "{\"value\": \"herp\", \"prop\": \"derp\"}"),
                Case(new Dictionary<int, string>() { { 0, "herp" }, { 1, "derp" } },
                    "[{\"Key\": 0, \"Value\": \"herp\"}, {\"Key\": 1, \"Value\": \"derp\"}]"),
                Case(new ObjectWithDictField<string, string>() { Dict = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } } },
                    "{\"Dict\": {\"value\": \"herp\", \"prop\": \"derp\"}}"),
                Case(new ObjectWithDictField<JSONEnum, string>() { Dict = new Dictionary<JSONEnum, string>() { { JSONEnum.One, "herp" }, { JSONEnum.Two, "derp" } } },
                    "{\"Dict\": {\"One\": \"herp\", \"Two\": \"derp\"}}"),
                Case(new GenericClass<string>() { Value = "herp", Prop = "derp" },
                    "{\"Value\": \"herp\", \"Prop\": \"derp\"}"),
                Case(new PropClass() { Property = 3 },
                    "{\"Property\": 3}"),
            };
        }
        [TestCaseSource(nameof(ObjectArgs))]
        public void ObjectSerDes(Args args) {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            model[typeof(JSONEnum)].SetSurrogate(Surrogate.EnumAsString<JSONEnum>());
            args.SerDes(new JSONSerializer(model));
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
        [ReplicateType]
        public class ObjectWithDefaultedDictField {
            [Replicate]
            public Dictionary<int, string> Dict = new Dictionary<int, string> { { 1, "herp" }, { 2, "derp" } };
        }
        [ReplicateType]
        public class ObjectWithDefaultedDictField2 {
            [Replicate]
            public Dictionary<int, string> Dict = new Dictionary<int, string> { { 1, "herp" }, { 3, "herp" } };
        }
        [Test]
        public void DefaultedDictionary() {
            var obj = new ObjectWithDefaultedDictField();
            obj.Dict[1] = "faff";
            var ser = new JSONSerializer(new ReplicationModel() { });
            var str = ser.SerializeString(obj);
            var output = ser.Deserialize<ObjectWithDefaultedDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
            var output2 = ser.Deserialize<ObjectWithDefaultedDictField2>(str);
            Assert.AreEqual(output2.Dict[1], "faff");
            Assert.AreEqual(output2.Dict[2], "derp");
            Assert.AreEqual(output2.Dict[3], "herp");
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
            model[typeof(ObjectWithDictField<string, string>)].SetSurrogate(typeof(ObjectWithDictFieldSurrogate));
            var ser = new JSONSerializer(model);
            var obj = new ObjectWithDictField<string, string>() {
                Dict = new Dictionary<string, string>()
                {
                    {"a", "herp" },
                    {"b", "derp" },
                },
            };
            var serStr = ser.SerializeString(obj);
            Assert.AreEqual("{\"Dict\": {\"afaff\": \"herp\", \"bfaff\": \"derp\"}}", serStr);
            var deser = ser.Deserialize<ObjectWithDictField<string, string>>(serStr);
            CollectionAssert.AreEqual(obj.Dict, deser.Dict);
        }
        [Test]
        public void HandlesExtraObjectFields() {
            var ser = new JSONSerializer(new ReplicationModel(), new JSONSerializer.Configuration { Strict = false });
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
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class Parent { public Parent() { } public Parent(float herp) { Herp = herp; } public float Herp { get; private set; } }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class ClassWithParent { public Parent Parent; }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class ChildA : Parent { public ChildA() { } public ChildA(float herp) : base(herp) { } public int Field = 3; }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
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
            [Replicate]
            public ChildStruct Child;
        }
        [ReplicateType]
        public struct ChildStruct {
            [Replicate]
            public int Num;
        }
        [ReplicateType]
        public struct ChildWithSurrogate {
            [Replicate]
            public int Num;
        }
        [Test]
        public void NestedStructType() {
            var model = new ReplicationModel(false) { DictionaryAsObject = true };
            model.Add(typeof(ChildStruct));
            model.Add(typeof(ChildWithSurrogate)).SetSurrogate(Surrogate.Simple<ChildWithSurrogate, int>(c => c.Num, n => new ChildWithSurrogate() { Num = n }));
            model.Add(typeof(ParentStruct));
            var ser = new JSONSerializer(model);
            var str = ser.SerializeString(new ParentStruct() { Child = new ChildStruct() { Num = 0xFAFF } });
            Assert.AreEqual(str, "{\"Child\": {\"Num\": 64255}}");
            str = ser.SerializeString(new Dictionary<string, KeyValuePair<string, ChildStruct>> { { "a", new KeyValuePair<string, ChildStruct>("b", new ChildStruct() { Num = 0xFAFF }) } });
            Assert.AreEqual(str, "{\"a\": {\"Key\": \"b\", \"Value\": {\"Num\": 64255}}}");
            str = ser.SerializeString(new Dictionary<string, KeyValuePair<string, ChildWithSurrogate>> { { "a", new KeyValuePair<string, ChildWithSurrogate>("b", new ChildWithSurrogate() { Num = 0xFAFF }) } });
            Assert.AreEqual(str, "{\"a\": {\"Key\": \"b\", \"Value\": 64255}}");
        }
        [Test]
        public void ExplicityMembersType() {
            var model = new ReplicationModel();
            var ser = new JSONSerializer(model);
            var obj = new ExplicitMembersType() { FirstString = "A", SecondString = "B" };
            var json = ser.SerializeString(obj);
            var deser = ser.Deserialize<ExplicitMembersType>(json);
            Assert.AreEqual(obj.FirstString, deser.FirstString);
        }
    }
}
