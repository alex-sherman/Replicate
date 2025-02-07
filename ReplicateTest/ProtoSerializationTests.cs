using System;
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
    public class ProtoSerializationTests {
        #region Types
        [ReplicateType]
        public enum ProtoEnum {
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
            [Replicate(1)]
            public uint Property { get; set; }
        }
        [ReplicateType]
        public class ObjectWithArrayField {
            public List<double> ArrayField;
        }
        [ReplicateType]
        public class ObjectWithNullableField {
            [SkipNull]
            public uint? NullableValue;
        }
        [ReplicateType]
        public class SubClass : PropClass {
            [Replicate(2)]
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
                0x12, 4, (byte)'f', (byte)'a', (byte)'f', (byte)'f'
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
        [TestCase(true, typeof(bool), new byte[] { 1 })]
        [TestCase(false, typeof(bool), new byte[] { 0 })]
        [TestCase(0, typeof(int), new byte[] { 0 })]
        [TestCase(1, typeof(int), new byte[] { 2 })]
        [TestCase(1, typeof(uint), new byte[] { 1 })]
        [TestCase(-1, typeof(int), new byte[] { 1 })]
        [TestCase(1.02f, typeof(float), new byte[] { 0x5c, 0x8f, 0x82, 0x3f })]
        [TestCase(1.02, typeof(double), new byte[] { 0x52, 0xB8, 0x1E, 0x85, 0xEB, 0x51, 0xF0, 0x3F })]
        [TestCase(300, typeof(uint), new byte[] { 0xAC, 0x02 })]
        [TestCase("faff", typeof(string), new byte[] { (byte)'f', (byte)'a', (byte)'f', (byte)'f' })]
        public void SerializeDeserialize(object obj, Type type, byte[] serialized) {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(type, obj);
            CollectionAssert.AreEqual(serialized, bytes);
            var output = ser.Deserialize(type, bytes);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void NullableNullInt() {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes<int?>(null);
            Assert.IsTrue(bytes.Length == 0);
        }
        [Test]
        public void RepeatedInt() {
            var ser = new ProtoSerializer(new ReplicationModel());
            ObjectWithArrayField arrayObj = new ObjectWithArrayField() {
                ArrayField = new List<double> { 1, 2, 3 }
            };
            var bytes = ser.SerializeBytes(arrayObj);
            var output = ser.Deserialize<ObjectWithArrayField>(bytes);
            CollectionAssert.AreEqual(arrayObj.ArrayField, output.ArrayField);
        }
        [Test]
        public void Dictionary() {
            var obj = new ObjectWithDictField() { Dict = new Dictionary<string, string> { { "value", "herp" }, { "prop", "derp" } } };
            var ser = new ProtoSerializer(new ReplicationModel() { });
            var str = ser.SerializeString(obj);
            var output = ser.Deserialize<ObjectWithDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
        }
        [ReplicateType]
        class ObjectWithDefaultedDictField {
            public Dictionary<string, string> Dict = new Dictionary<string, string> { { "value", "herp" }, { "prop", "derp" } };
        }
        [Test]
        public void DefaultedDictionary() {
            var obj = new ObjectWithDefaultedDictField();
            obj.Dict["value"] = "faff";
            var ser = new ProtoSerializer(new ReplicationModel() { });
            var str = ser.SerializeString(obj);
            var output = ser.Deserialize<ObjectWithDefaultedDictField>(str);
            Assert.AreEqual(obj.Dict, output.Dict);
        }
    }
}
