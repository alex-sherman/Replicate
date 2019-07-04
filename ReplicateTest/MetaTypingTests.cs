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
        [Test]
        public void TestCanSetFields()
        {
            var newType = Fake.FromType(typeof(MockType));
            var obj = Activator.CreateInstance(newType);
            var node = ReplicationModel.Default.GetRepNode(obj, newType).AsObject;
            var derp = node["Derp"] = new RepBackedNode("herp");
            Assert.AreEqual("herp", node["Derp"].RawValue);
        }
        [Test]
        public void TestCanSetReadonlyProperties()
        {
            var newType = Fake.FromType(typeof(MockType));
            var obj = Activator.CreateInstance(newType);
            var node = ReplicationModel.Default.GetRepNode(obj, newType).AsObject;
            var derp = node["Herp"] = new RepBackedNode(0xFAFF);
            Assert.AreEqual(0xFAFF, node["Herp"].RawValue);
        }
    }
}
