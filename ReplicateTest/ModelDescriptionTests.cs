using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaTyping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [ReplicateType]
    public class CustomType
    {
        public string Field;
    }
    [TestFixture]
    class ModelDescriptionTests
    {
        [Test]
        public void TestFakeGetsMarked()
        {
            var model = new ReplicationModel(false, false)
            {
                typeof(string),
                typeof(CustomType)
            };
            model.Add(Fake.FromType(typeof(CustomType), model));
            var desc = model.GetDescription();
            Assert.IsNull(desc.Types[1].FakeSourceType);
            Assert.AreEqual("ReplicateTest.CustomType", desc.Types[2].FakeSourceType);
        }
        [Test]
        public void TestApplyingAddsNewType()
        {
            var model = new ReplicationModel();
            Assert.NotNull(model[typeof(CustomType)]);
            var secondModel = new ReplicationModel(false);
            Assert.Catch<KeyNotFoundException>(() => { var derp = secondModel[typeof(CustomType)]; });
            secondModel.LoadFrom(model.GetDescription());
            Assert.NotNull(secondModel[typeof(CustomType)]);
        }
    }
}
