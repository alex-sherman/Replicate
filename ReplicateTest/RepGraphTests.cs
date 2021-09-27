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
        //[ReplicatePolicy(AsReference = true)]
        //public ObjectType ReferenceField;
    }
    [ReplicateType]
    public struct ValueType
    {
        public string Field1;
    }

    [ReplicateType]
    public class SurrogateType
    {
        public string OtherField;
        public static implicit operator ObjectType(SurrogateType @this)
        {
            return new ObjectType() { Field1 = @this.OtherField };
        }
        public static implicit operator SurrogateType(ObjectType other)
        {
            return new SurrogateType() { OtherField = other.Field1 };
        }
    }

    [TestFixture]
    public class RepGraphTests
    {
        [Test]
        public void PrimitiveString()
        {
            var value = new RepBackedNode("herpderp").AsPrimitive;
            Assert.AreEqual(MarshallMethod.Primitive, value.MarshallMethod);
            Assert.AreEqual(PrimitiveType.String, value.PrimitiveType);
        }
        [Test]
        public void PrimitiveNullableInt()
        {
            var value = new RepBackedNode((int?)1).AsPrimitive;
            Assert.AreEqual(MarshallMethod.Primitive, value.MarshallMethod);
            Assert.AreEqual(PrimitiveType.SVarInt, value.PrimitiveType);
        }
        [Test]
        public void PrimitiveByte()
        {
            var value = new RepBackedNode((byte)1).AsPrimitive;
            Assert.AreEqual(MarshallMethod.Primitive, value.MarshallMethod);
            Assert.AreEqual(PrimitiveType.Byte, value.PrimitiveType);
        }
        [Test]
        public void ObjectStringField()
        {
            var value = new RepBackedNode(new ObjectType() { Field1 = "hurrdurr", Field2 = null }, model: new ReplicationModel()).AsObject;
            Assert.AreEqual(MarshallMethod.Object, value.MarshallMethod);
            var field1 = value["Field1"].AsPrimitive;
            Assert.AreEqual(MarshallMethod.Primitive, field1.MarshallMethod);
            Assert.AreEqual(PrimitiveType.String, field1.PrimitiveType);
            Assert.AreEqual("hurrdurr", field1.Value);
        }
        [Test]
        public void DictionaryAsObjectTrue()
        {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            var node = model.GetRepNode(new Dictionary<string, string>() { { "value", "herp" }, { "prop", "derp" } }, null, null);
            Assert.AreEqual(MarshallMethod.Object, node.MarshallMethod);
            Assert.AreEqual("herp", node.AsObject["value"].AsPrimitive.Value);
            Assert.AreEqual("derp", node.AsObject["prop"].AsPrimitive.Value);
        }
        [Test]
        public void UpdateStringField()
        {
            var obj = new ObjectType() { Field1 = "hurrdurr", Field2 = null };
            var objNode = new RepBackedNode(obj, model: new ReplicationModel()).AsObject;
            var value = objNode["Field1"];
            value.Value = "newvalue";
            objNode["Field1"] = value;
            Assert.AreEqual("newvalue", obj.Field1);
        }
        [Test]
        public void GetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj, model: new ReplicationModel()).AsObject["EnumerableField"].AsCollection.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("herp", listValue[0].AsObject["Field1"].AsPrimitive.Value);
        }
        [Test]
        public void GetEnumerableFieldValues()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var collection = new RepBackedNode(obj, model: new ReplicationModel()).AsObject["EnumerableField"].AsCollection;
            var listValue = collection.Values.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("herp", (listValue[0] as ObjectType).Field1);
            collection.Values = new[] { new ObjectType() { Field1 = "derp" } };
            listValue = collection.Values.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("derp", (listValue[0] as ObjectType).Field1);
        }
        [Test]
        public void SetListField()
        {
            var obj = new ObjectType() { ListField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var objNode = new RepBackedNode(obj, model: new ReplicationModel()).AsObject;
            var listValue = objNode["ListField"].AsCollection;
            listValue.Values = new[] { new ObjectType() { Field1 = "derp" } };
            objNode["ListField"] = listValue;
            Assert.AreEqual("derp", obj.ListField[0].Field1);
        }
        [Test]
        public void SetArrayField()
        {
            var obj = new ObjectType() { ArrayField = new[] { new ObjectType() { Field1 = "herp" } } };
            var objNode = new RepBackedNode(obj, model: new ReplicationModel()).AsObject;
            var listValue = objNode["ArrayField"].AsCollection;
            listValue.Values = new List<ObjectType>() { new ObjectType() { Field1 = "derp" } };
            objNode["ArrayField"] = listValue;
            Assert.AreEqual("derp", obj.ArrayField[0].Field1);
        }
        [Test]
        public void SetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var objNode = new RepBackedNode(obj, model: new ReplicationModel()).AsObject;
            var listValue = objNode["EnumerableField"].AsCollection;
            listValue.Values = new[] { new ObjectType() { Field1 = "derp" } };
            objNode["EnumerableField"] = listValue;
            Assert.AreEqual("derp", obj.EnumerableField.ToList()[0].Field1);
        }
        // TODO: Fix this with the decoupling of manager/collection for replicated objects?
        //[Test]
        //public void GetReferenceTypeField()
        //{
        //    var obj = new ObjectType() { ReferenceField = new ObjectType() { Field1 = "herp" } };
        //    var referenceValue = new RepBackedNode(obj).AsObject["ReferenceField"].AsObject.Value as ReplicatedReference<ObjectType>;
        //    Assert.IsNotNull(referenceValue);
        //}
        [Test]
        public void GetSurrogateTypeField()
        {
            var model = new ReplicationModel();
            model[typeof(ObjectType)].Members.Values.First(m => m.Name == "Field2")
                .SetSurrogate(typeof(SurrogateType));

            var obj = new ObjectType() { Field2 = new ObjectType() { Field1 = "Herp" } };
            var surrogateValue = new RepBackedNode(obj, model: model).AsObject["Field2"].AsObject;
            Assert.AreEqual(typeof(SurrogateType), surrogateValue.Value.GetType());
            Assert.AreEqual("Herp", surrogateValue["OtherField"].AsPrimitive.Value);
        }
        [Test]
        public void SetSurrogateTypeField()
        {
            var model = new ReplicationModel();
            model[typeof(ObjectType)].Members.Values.First(m => m.Name == "Field2")
                .SetSurrogate(typeof(SurrogateType));

            var obj = new ObjectType() { Field2 = new ObjectType() { Field1 = "Herp" } };
            var objNode = new RepBackedNode(obj, model: model).AsObject;
            var surrogateValue = objNode["Field2"].AsObject;
            surrogateValue.Value = new SurrogateType() { OtherField = "FAFF" };
            objNode["Field2"] = surrogateValue;
            Assert.AreEqual("FAFF", obj.Field2.Field1);
        }
        [Test]
        public void TestNullValue()
        {
            var model = new ReplicationModel();
            var node = model.GetRepNode(null, typeof(ObjectType));
            Assert.AreEqual(MarshallMethod.Object, node.MarshallMethod);
            var field1 = node.AsObject["Field1"];
            Assert.AreEqual(MarshallMethod.Primitive, field1.MarshallMethod);
            Assert.AreEqual(PrimitiveType.String, field1.AsPrimitive.PrimitiveType);
        }
        [Test]
        public void TestSettingNullValue()
        {
            var model = new ReplicationModel();
            var node = model.GetRepNode(null, typeof(ObjectType));
            node.Value = new ObjectType() { Field1 = "DERP" };
            Assert.AreEqual("DERP", node.AsObject["Field1"].Value);
        }
        [Test]
        public void TestSettingValueTypeField()
        {
            var model = new ReplicationModel();
            var node = model.GetRepNode(new ValueType() { Field1 = "DERP" }, typeof(ValueType)).AsObject;
            Assert.AreEqual("DERP", node["Field1"].Value);
            node["Field1"] = new RepBackedNode("HERP");
            var value = (ValueType)node.RawValue;
            Assert.AreEqual("HERP", value.Field1);
        }
        [Test]
        public void TestGettingRepNodeOfRepNode()
        {
            var model = new ReplicationModel();
            var node = model.GetRepNode(new RepBackedNode(new ValueType() { Field1 = "DERP" }, model: model), typeof(IRepNode));
            Assert.IsInstanceOf<RepBackedNode>(node);
            Assert.IsInstanceOf<RepBackedNode>(node.AsObject["Field1"]);
        }
        [Test]
        public void TestGetRepNodeWithoutBackingAndTypeAccessorFails()
        {
            var model = new ReplicationModel();
            Assert.Throws<InvalidOperationException>(() => model.GetRepNode(null, null, null));
        }
    }
}
