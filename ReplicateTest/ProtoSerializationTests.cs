﻿using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaData.Policy;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using static ReplicateTest.SerializerTest;

namespace ReplicateTest {
    [TestFixture]
    public class ProtoSerializationTests {
        #region Types
        [ReplicateType]
        public enum ProtoEnum {
            One = 1,
            Two = 2,
        }
        [ReplicateType]
        public class ObjectWithDictFieldSurrogate {
            public Dictionary<string, string> Dict;
            public static implicit operator ObjectWithDictField<string, string>(ObjectWithDictFieldSurrogate @this) {
                return new ObjectWithDictField<string, string>() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key.Replace("faff", ""), kvp => kvp.Value) };
            }
            public static implicit operator ObjectWithDictFieldSurrogate(ObjectWithDictField<string, string> @this) {
                return new ObjectWithDictFieldSurrogate() { Dict = @this.Dict?.ToDictionary(kvp => kvp.Key + "faff", kvp => kvp.Value) };
            }
        }
        #endregion

        [Test]
        public void SerDesProperty() {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(new PropClass() { Property = 3 });
            Assert.AreEqual(new byte[] { 0x08, 0x03 }, bytes);
            var obj = ser.Deserialize<PropClass>(bytes);
            Assert.AreEqual(3, obj.Property);
        }
        [Test]
        public void SerDesSubClass() {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(new SubClass() { Property = 3, Field = "faff" });
            Assert.AreEqual(new byte[] {
                0x08, 0x03,
                0x12, 0x04, (byte)'f', (byte)'a', (byte)'f', (byte)'f',
                0x1A, 0x00
            }, bytes);
            var obj = ser.Deserialize<SubClass>(bytes);
            Assert.AreEqual(3, obj.Property);
            Assert.AreEqual("faff", obj.Field);
        }
        [Test]
        public void SerDesUnknownString() {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = new byte[] { 0x12, 0x05, 0, 0, 0, 0, 0, 0x08, 0x03 };
            var obj = ser.Deserialize<PropClass>(bytes);
            Assert.AreEqual(3, obj.Property);
        }

        public static IEnumerable<Args> SimpleArgs() {
            return new[] {
                Case(true, 1),
                Case(false, 0),
                Case((int?)0, 0),
                Case((int?)0x123, 0xC6, 0x04),
                Case(1u, 1),
                Case(1, 2),
                Case(19.41428f, 0x72, 0x50, 0x9B, 0x41),
                Case(300u, 0xAC, 0x02),
                Case((short)-1, 1),
                Case((ushort)0xfaff, 0xFF, 0xF5, 0x03),
                Case(0.5f, 0x00, 0x00, 0x00, 0x3F),
                Case(0.5d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x3F),
                Case(1.02f, 0x5c, 0x8f, 0x82, 0x3f),
                Case(1.02d, 0x52, 0xB8, 0x1E, 0x85, 0xEB, 0x51, 0xF0, 0x3F),
                Case((float?)0, 0x00, 0x00, 0x00, 0x00),
                Case((float?)1, 0x00, 0x00, 0x80, 0x3F),
                Case(""),
                Case("😈", 0xF0, 0x9F, 0x98, 0x88),
                Case("\"", (byte)'\"'),
                Case("faff", (byte)'f', (byte)'a', (byte)'f', (byte)'f'),
                Case(76561198000857376L, 0xC0, 0x84, 0xDB, 0xA6, 0xA0, 0x80, 0x80, 0x90, 0x02),
                Case(JSONEnum.One,1),
                Case(JSONEnum.Two, 2),
                Case(new Guid("dcae97d8-0315-4aa8-94fe-92a91e5983bc"), "dcae97d8-0315-4aa8-94fe-92a91e5983bc".Select(c => (byte)c).ToArray()),
            };
        }
        [TestCaseSource(nameof(SimpleArgs))]
        public void SerDes(Args args) {
            args.SerDes(new ProtoSerializer(new ReplicationModel()));
        }
        public static IEnumerable<Args> CollectionArgs() {
            return new[] {
                //Case(new Collection<int> { Array = new int[] { 1, 2, 3, 4 } }),
                Case(new Collection<int> { List = new List<int> { 1, 2, 3, 4 } }),
                Case(new Collection<double> { List = new List<double> { 1, 2, 3, 4 } }),
                Case(new Collection<int> { ICollection = new List<int> { 1, 2, 3, 4 } }),
                Case(new Collection<int> { IEnumerable = new List<int> { 1, 2, 3, 4 } }),
                Case(new Collection<int> { HashSet = new HashSet<int> { 1, 2, 3, 4 } }),
                Case(new Collection<JSONEnum> { List = new List<JSONEnum> { JSONEnum.Three, JSONEnum.Two } }),
            };
        }
        [TestCaseSource(nameof(CollectionArgs))]
        public void CollectionSerDes(Args args) {
            args.SerDes(new ProtoSerializer(new ReplicationModel()));
        }

        public static IEnumerable<Args> ObjectArgs() {
            return new[] {
                Case(new SubClass()),
                Case(new SubClass() { Field = "derp" }),
                Case(new SubClass() { Property = 2 }),
                Case(new StructType() { Double = 2.2 }),
                Case(new ObjectWithDictField<string, string>() { Dict = new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } } }),
                Case(new ObjectWithDictField<JSONEnum, string>() { Dict = new Dictionary<JSONEnum, string>() { { JSONEnum.One, "herp" }, { JSONEnum.Two, "derp" } } }),
                Case(new GenericClass<string>() { Value = "herp", Prop = "derp" }),
                Case(new PropClass() { Property = 3 }),
            };
        }
        [TestCaseSource(nameof(ObjectArgs))]
        public void ObjectSerDes(Args args) {
            args.SerDes(new ProtoSerializer(new ReplicationModel() { DictionaryAsObject = true }));
        }
        [ReplicateType]
        class ObjectWithDefaultedDictField {
            [Replicate]
            public Dictionary<string, string> Dict = new Dictionary<string, string> { { "value", "herp" }, { "prop", "derp" } };
        }
        [ReplicateType]
        class ObjectWithDefaultedDictField2 {
            [Replicate]
            public Dictionary<string, string> Dict = new Dictionary<string, string> { { "value", "herp" }, { "new_prop", "herp" } };
        }
        [Test]
        public void DefaultedDictionary() {
            var obj = new ObjectWithDefaultedDictField();
            obj.Dict["value"] = "faff";
            var ser = new ProtoSerializer(new ReplicationModel() { });
            var str = ser.SerializeBytes(obj);
            var output = ser.Deserialize<ObjectWithDefaultedDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
            var output2 = ser.Deserialize<ObjectWithDefaultedDictField2>(str);
            Assert.AreEqual(output2.Dict["value"], "faff");
            Assert.AreEqual(output2.Dict["prop"], "derp");
            Assert.AreEqual(output2.Dict["new_prop"], "herp");
        }
    }
}
