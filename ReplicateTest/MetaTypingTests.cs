using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaTyping;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class MetaTypingTests
    {
        [ReplicateType]
        public class MockType
        {
            public string Derp;
            public int? Herp { get; }
        }
        [ReplicateType]
        public class MockTypeGeneric<T>
        {
            public T Derp;
        }
        [Test]
        public void TestCanSetFields()
        {
            var model = new ReplicationModel();
            var newType = Fake.FromType(typeof(MockType), model);
            model.Add(newType);
            var obj = Activator.CreateInstance(newType);
            var node = model.GetRepNode(obj, newType).AsObject;
            node["Derp"] = new RepBackedNode("herp");
            Assert.AreEqual("herp", node["Derp"].RawValue);
        }
        [Test]
        public void TestCanSetReadonlyProperties()
        {
            var model = new ReplicationModel();
            var newType = Fake.FromType(typeof(MockType), model);
            model.Add(newType);
            var obj = Activator.CreateInstance(newType);
            var node = model.GetRepNode(obj, newType).AsObject;
            node["Herp"] = new RepBackedNode(0xFAFF);
            Assert.AreEqual(0xFAFF, node["Herp"].RawValue);
        }
        [Test]
        public void TestCanSetGenerics()
        {
            var model = new ReplicationModel();
            var newType = Fake.FromType(typeof(MockTypeGeneric<>), model).MakeGenericType(typeof(string));
            model.Add(newType);
            var obj = Activator.CreateInstance(newType);
            var node = model.GetRepNode(obj, newType).AsObject;
            node["Derp"] = new RepBackedNode("herp");
            Assert.AreEqual("herp", node["Derp"].RawValue);
        }
        [Test]
        public void TestKVPSurrogateWorks()
        {
            var node = ReplicationModel.Default.GetRepNode(null, typeof(KeyValuePair<string, int>)).AsObject;
            node.EnsureConstructed();
            node["Key"] = new RepBackedNode("Derp");
            KeyValuePair<string, int> derp = (KeyValuePair<string, int>)node.RawValue;
            Assert.AreEqual("Derp", derp.Key);
        }
        [Test]
        public void TestIRepNodeResolve()
        {
            var repNodeTypeData = ReplicationModel.Default[typeof(IRepNode)];
            Assert.AreEqual(repNodeTypeData, ReplicationModel.Default[typeof(RepBackedNode)]);
            Assert.AreEqual(repNodeTypeData, ReplicationModel.Default[typeof(RepDictObject<>)]);
            Assert.AreEqual(repNodeTypeData, ReplicationModel.Default[typeof(RepNodeTypeless)]);
        }
    }
}
