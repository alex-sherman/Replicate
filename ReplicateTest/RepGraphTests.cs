using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [ReplicateType]
    public class ObjectType
    {
        public string Field1;
        public ObjectType Field2;
        public List<ObjectType> ListField;
        public ObjectType[] ArrayField;
        public IEnumerable<ObjectType> EnumerableField;
    }

    [TestFixture]
    public class RepGraphTests
    {
        [Test]
        public void TestPrimitiveString()
        {
            var value = new RepBackedNode("herpderp").AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.String, value.PrimitiveType);
        }
        [Test]
        public void TestPrimitiveNullableInt()
        {
            var value = new RepBackedNode((int?)1).AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.Int32, value.PrimitiveType);
        }
        [Test]
        public void TestPrimitiveByte()
        {
            var value = new RepBackedNode((byte)1).AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.Int8, value.PrimitiveType);
        }
        [Test]
        public void TestObjectStringField()
        {
            var value = new RepBackedNode(new ObjectType() { Field1 = "hurrdurr", Field2 = null }).AsObject;
            Assert.AreEqual(MarshalMethod.Object, value.MarshalMethod);
            var field1 = value["Field1"].AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, field1.MarshalMethod);
            Assert.AreEqual(PrimitiveType.String, field1.PrimitiveType);
            Assert.AreEqual("hurrdurr", field1.Value);
        }
        [Test]
        public void TestUpdateStringField()
        {
            var obj = new ObjectType() { Field1 = "hurrdurr", Field2 = null };
            var value = new RepBackedNode(obj).AsObject;
            var field1 = value["Field1"].AsPrimitive;
            field1.Value = "newvalue";
            Assert.AreEqual("newvalue", obj.Field1);
        }
        [Test]
        public void TestGetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["EnumerableField"].AsCollection.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("herp", listValue[0].AsObject["Field1"].AsPrimitive.Value);
        }
        [Test]
        public void TestGetEnumerableFieldValues()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var collection = new RepBackedNode(obj).AsObject["EnumerableField"].AsCollection;
            var listValue = collection.Value.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("herp", (listValue[0] as ObjectType).Field1);
            collection.Value = new[] { new ObjectType() { Field1 = "derp" } };
            listValue = collection.Value.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("derp", (listValue[0] as ObjectType).Field1);
        }
        [Test]
        public void TestSetListField()
        {
            var obj = new ObjectType() { ListField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["ListField"].AsCollection;
            listValue.Value = new[] { new ObjectType() { Field1 = "derp" } };
            Assert.AreEqual("derp", obj.ListField[0].Field1);
        }
        [Test]
        public void TestSetArrayField()
        {
            var obj = new ObjectType() { ArrayField = new[] { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["ArrayField"].AsCollection;
            listValue.Value = new List<ObjectType>() { new ObjectType() { Field1 = "derp" } };
            Assert.AreEqual("derp", obj.ArrayField[0].Field1);
        }
        [Test]
        public void TestSetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["EnumerableField"].AsCollection;
            listValue.Value = new[] { new ObjectType() { Field1 = "derp" } };
            Assert.AreEqual("derp", obj.EnumerableField.ToList()[0].Field1);
        }
    }
}
