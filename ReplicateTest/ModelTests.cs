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
        public string Property { get; set; }
    }
    [TestFixture]
    public class ModelTests
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
            Assert.False(desc.Types[1].IsFake);
            Assert.True(desc.Types[2].IsFake);
            Assert.AreEqual(2, desc.Types[2].Members.Count);
            var field = desc.Types[2].Members[0];
            Assert.AreEqual("Field", field.Key.Name);
            var property = desc.Types[2].Members[1];
            Assert.AreEqual("Property", property.Key.Name);
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
            var testValue = new CustomType() { Field = "Derp", Property = "Herp" };
            var fieldValue = secondModel.GetTypeAccessor(typeof(CustomType)).Members["Field"].GetValue(testValue);
            Assert.AreEqual("Derp", fieldValue);
            var propertyValue = secondModel.GetTypeAccessor(typeof(CustomType)).Members["Property"].GetValue(testValue);
            Assert.AreEqual("Herp", propertyValue);
        }
        [Test]
        public void CreatesFakeForMissingTypes()
        {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription()
            {
                Key = new RepKey(model1.Types.Count, "Faff"),
                Members = new List<MemberDescription>()
                {
                    new MemberDescription()
                    {
                        Key = new RepKey(0, "Derp"),
                        TypeId = stringId.Id,
                    }
                }
            });
            var model2 = new ReplicationModel(false);
            model2.LoadFrom(desc);
            var faffType = model2.Types["Faff"];
            Assert.NotNull(faffType);
            var derpMember = faffType["Derp"];
            Assert.NotNull(derpMember);
            Assert.AreEqual(typeof(string), derpMember.MemberType);
        }
        [Test]
        public void CreatesRecursiveFakeForMissingTypes()
        {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription()
            {
                Key = new RepKey(model1.Types.Count, "Faff"),
                Members = new List<MemberDescription>()
                {
                    new MemberDescription()
                    {
                        Key = new RepKey(0, "Derp"),
                        TypeId = new RepKey(model1.Types.Count, "Faff"),
                    }
                }
            });
            var model2 = new ReplicationModel(false);
            model2.LoadFrom(desc);
            var faffType = model2.Types["Faff"];
            Assert.NotNull(faffType);
            var derpMember = faffType["Derp"];
            Assert.NotNull(derpMember);
            Assert.AreEqual(typeof(string), derpMember.MemberType);
        }
        [Test]
        public void InvalidSetKey()
        {
            var set = new RepSet<string>();
            Assert.Throws<InvalidOperationException>(() => set["derp"] = null);
        }
        [Test]
        public void ValidSetKeyRetrieves()
        {
            var set = new RepSet<string>();
            set[new RepKey(0, "derp")] = "derp";
            Assert.AreEqual("derp", set["derp"]);
            Assert.AreEqual("derp", set[0]);
        }
        [Test]
        public void TestSparseSet()
        {
            var set = new RepSet<string>();
            set[new RepKey(5, "derp")] = "derp";
            Assert.AreEqual("derp", set["derp"]);
            Assert.AreEqual(null, set[0]);
            Assert.AreEqual(null, set[1]);
            Assert.AreEqual(null, set[2]);
            Assert.AreEqual(null, set[3]);
            Assert.AreEqual(null, set[4]);
            Assert.AreEqual("derp", set[5]);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var derp = set[6]; });
        }
        [Test]
        public void ToRepSet()
        {
            var set = new RepSet<string>();
            set[new RepKey(0, "derp")] = "derp";
            var set2 = set.Select(v => new KeyValuePair<RepKey, string>(v.Key, "herp" + v.Value)).ToRepSet();
            Assert.AreEqual("herpderp", set2[0]);
            Assert.AreEqual("herpderp", set2["derp"]);
        }
    }
}
