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
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.String, value.PrimitiveType);
        }
        [Test]
        public void PrimitiveNullableInt()
        {
            var value = new RepBackedNode((int?)1).AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.Int32, value.PrimitiveType);
        }
        [Test]
        public void PrimitiveByte()
        {
            var value = new RepBackedNode((byte)1).AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, value.MarshalMethod);
            Assert.AreEqual(PrimitiveType.Int8, value.PrimitiveType);
        }
        [Test]
        public void ObjectStringField()
        {
            var value = new RepBackedNode(new ObjectType() { Field1 = "hurrdurr", Field2 = null }).AsObject;
            Assert.AreEqual(MarshalMethod.Object, value.MarshalMethod);
            var field1 = value["Field1"].AsPrimitive;
            Assert.AreEqual(MarshalMethod.Primitive, field1.MarshalMethod);
            Assert.AreEqual(PrimitiveType.String, field1.PrimitiveType);
            Assert.AreEqual("hurrdurr", field1.Value);
        }
        [Test]
        public void UpdateStringField()
        {
            var obj = new ObjectType() { Field1 = "hurrdurr", Field2 = null };
            var value = new RepBackedNode(obj).AsObject;
            var field1 = value["Field1"].AsPrimitive;
            field1.Value = "newvalue";
            Assert.AreEqual("newvalue", obj.Field1);
        }
        [Test]
        public void GetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["EnumerableField"].AsCollection.ToList();
            Assert.AreEqual(1, listValue.Count);
            Assert.AreEqual("herp", listValue[0].AsObject["Field1"].AsPrimitive.Value);
        }
        [Test]
        public void GetEnumerableFieldValues()
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
        public void SetListField()
        {
            var obj = new ObjectType() { ListField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["ListField"].AsCollection;
            listValue.Value = new[] { new ObjectType() { Field1 = "derp" } };
            Assert.AreEqual("derp", obj.ListField[0].Field1);
        }
        [Test]
        public void SetArrayField()
        {
            var obj = new ObjectType() { ArrayField = new[] { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["ArrayField"].AsCollection;
            listValue.Value = new List<ObjectType>() { new ObjectType() { Field1 = "derp" } };
            Assert.AreEqual("derp", obj.ArrayField[0].Field1);
        }
        [Test]
        public void SetEnumerableField()
        {
            var obj = new ObjectType() { EnumerableField = new List<ObjectType>() { new ObjectType() { Field1 = "herp" } } };
            var listValue = new RepBackedNode(obj).AsObject["EnumerableField"].AsCollection;
            listValue.Value = new[] { new ObjectType() { Field1 = "derp" } };
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
            model[typeof(ObjectType)].ReplicatedMembers.First(m => m.Name == "Field2")
                .SetSurrogate(typeof(SurrogateType));

            var obj = new ObjectType() { Field2 = new ObjectType() { Field1 = "Herp" } };
            var surrogateValue = new RepBackedNode(obj, model: model).AsObject["Field2"].AsObject;
            Assert.AreEqual(typeof(SurrogateType), surrogateValue.Value.GetType());
            Assert.AreEqual("Herp", surrogateValue["OtherField"].AsPrimitive.Value);
        }
    }
}
