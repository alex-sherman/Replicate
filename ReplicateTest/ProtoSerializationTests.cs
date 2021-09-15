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
            [Replicate(1)]
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
        public void SerDesProperty()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(new PropClass() { Property = 3 });
            Assert.AreEqual(new byte[] { 0x08, 0x03 }, bytes);
            var obj = ser.Deserialize<PropClass>(bytes);
            Assert.AreEqual(3, obj.Property);
        }
        [Test]
        public void SerDesUnknownString()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = new byte[] { 0x12, 0x05, 0, 0, 0, 0, 0, 0x08, 0x03 };
            var obj = ser.Deserialize<PropClass>(bytes);
            Assert.AreEqual(3, obj.Property);
        }
        [TestCase(0, typeof(int), new byte[] { 0 })]
        [TestCase(1, typeof(int), new byte[] { 1 })]
        [TestCase(300, typeof(int), new byte[] { 0xAC, 0x02 })]
        [TestCase("faff", typeof(string), new byte[] { 4, (byte)'f', (byte)'a', (byte)'f', (byte)'f' })]
        public void SerializeDeserialize(object obj, Type type, byte[] serialized)
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes(type, obj);
            CollectionAssert.AreEqual(serialized, bytes);
            var output = ser.Deserialize(type, bytes);
            Assert.AreEqual(obj, output);
        }
        [Test]
        public void NullableNullInt()
        {
            var ser = new ProtoSerializer(new ReplicationModel());
            var bytes = ser.SerializeBytes<int?>(null);
            Assert.IsTrue(bytes.Length == 0);
        }
    }
}
