using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaTyping;
using System.Collections;
using System.Collections.Generic;

namespace ReplicateTest {
    [ReplicateType]
    public class CustomType {
        [Replicate]
        public string Field;
        [Replicate]
        public string Property { get; set; }
    }
    namespace Nested {
        [ReplicateType]
        public class CustomType {
            [Replicate]
            public string Field;
            [Replicate]
            public string Property { get; set; }
        }
    }
    public class NonReplicateType {
        public int Derp;
    }
    public class NonReplicateEnumerable : IEnumerable<NonReplicateType> {
        public IEnumerator<NonReplicateType> GetEnumerator() {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return list.GetEnumerator();
        }
        private List<NonReplicateType> list = new List<NonReplicateType>();
    }
    [TestFixture]
    public class ModelTests {
        [Test]
        public void TestGetsByteArrayTypeId() {
            var model = new ReplicationModel();
            var typeId = model.GetId(typeof(byte[]));
            Assert.AreEqual(typeof(IEnumerable<byte>), model.GetType(typeId));
        }
        [Test]
        public void TestFakeGetsMarked() {
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
        public void TestApplyingAddsNewType() {
            var model = new ReplicationModel();
            Assert.NotNull(model[typeof(CustomType)]);
            var secondModel = new ReplicationModel(false);
            Assert.Catch<KeyNotFoundException>(() => { var derp = secondModel[typeof(CustomType)]; });
            secondModel.LoadFrom(model.GetDescription());
            Assert.NotNull(secondModel[typeof(CustomType)]);
            var testValue = new CustomType() { Field = "Derp", Property = "Herp" };
            var fieldValue = secondModel.GetTypeAccessor(typeof(CustomType))["Field"].GetValue(testValue);
            Assert.AreEqual("Derp", fieldValue);
            var propertyValue = secondModel.GetTypeAccessor(typeof(CustomType))["Property"].GetValue(testValue);
            Assert.AreEqual("Herp", propertyValue);
        }
        [Test]
        public void SurrogatedCollectionWithNonReplicatedElementType() {
            var model = new ReplicationModel();
            Assert.NotNull(model[typeof(CustomType)]);
            Assert.NotNull(model[typeof(NonReplicateEnumerable)]);
            Assert.AreNotEqual(model[typeof(NonReplicateEnumerable)], model[typeof(IEnumerable<>)]);
            Assert.Catch<KeyNotFoundException>(
                () => { var derp = model[typeof(NonReplicateType)]; });
            // This doesn't need to fail, it's probably fine to return TypeAccessors for non-added types.
            Assert.Catch<KeyNotFoundException>(
                () => { var derp = model.GetTypeAccessor(typeof(NonReplicateEnumerable)); });
            model[typeof(NonReplicateEnumerable)].SetSurrogate(new Surrogate(typeof(List<int>)));
            Assert.NotNull(model.GetTypeAccessor(typeof(NonReplicateEnumerable)));
        }
        [Test]
        public void CreatesFakeForMissingTypes() {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription() {
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
        public void CreatesFakeForRecursiveMissingTypes() {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription() {
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
            Assert.AreEqual("Faff", derpMember.MemberType.Name);
        }
        [Test]
        public void CreatesFakeForGenericMissingTypes() {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription() {
                Key = new RepKey(model1.Types.Count, "Faff"),
                GenericParameters = new[] { "T" },
                Members = new List<MemberDescription>()
                {
                    new MemberDescription()
                    {
                        Key = new RepKey(0, "Derp"),
                        GenericPosition = 0,
                    }
                }
            });
            var model2 = new ReplicationModel(false);
            model2.LoadFrom(desc);
            var faffType = model2.Types["Faff"];
            Assert.NotNull(faffType);
            var genType = faffType.Type.MakeGenericType(typeof(string));
            var faffStr = model2.GetTypeAccessor(genType);
            Assert.AreEqual(typeof(string), faffStr.Members["Derp"].Type);
        }
        [Test]
        public void CreatesFakeForLaterMissingTypes() {
            var model1 = new ReplicationModel(false);
            var desc = model1.GetDescription();
            var stringId = model1.GetId(typeof(string));
            desc.Types.Add(new TypeDescription() {
                Key = new RepKey(model1.Types.Count, "Faff"),
                Members = new List<MemberDescription>()
                {
                    new MemberDescription()
                    {
                        Key = new RepKey(0, "Derp"),
                        TypeId = new RepKey(model1.Types.Count + 1, "Faff"),
                    }
                }
            });
            desc.Types.Add(new TypeDescription() {
                Key = new RepKey(model1.Types.Count + 1, "Herp"),
                Members = new List<MemberDescription>(),
            });
            var model2 = new ReplicationModel(false);
            model2.LoadFrom(desc);
            var faffType = model2.Types["Faff"];
            Assert.NotNull(faffType);
            var derpMember = faffType["Derp"];
            Assert.NotNull(derpMember);
            Assert.AreEqual("Herp", derpMember.MemberType.Name);
        }
        [Test]
        public void NamingOfTypes() {
            var model = new ReplicationModel(false, false)
            {
                typeof(string),
                typeof(CustomType)
            };
            Assert.AreEqual(model.Types["CustomType"]?.Type, typeof(CustomType));
            Assert.AreEqual(model.Types["ReplicateTest.CustomType"]?.Type, typeof(CustomType));
        }
        [Test]
        public void NamingOfTypesWithDuplicates() {
            var model = new ReplicationModel(false, false)
            {
                typeof(string),
                typeof(CustomType),
                typeof(Nested.CustomType)
            };
            Assert.AreEqual(model.Types["CustomType"]?.Type, typeof(CustomType));
            Assert.AreEqual(model.Types["ReplicateTest.CustomType"]?.Type, typeof(CustomType));
            Assert.AreEqual(model.Types["ReplicateTest.Nested.CustomType"]?.Type, typeof(Nested.CustomType));
        }
        [Test]
        public void NamingOfTypesWithDuplicatesAlternateOrder() {
            var model = new ReplicationModel(false, false)
            {
                typeof(string),
                typeof(Nested.CustomType),
                typeof(CustomType)
            };
            Assert.AreEqual(model.Types["CustomType"]?.Type, typeof(Nested.CustomType));
            Assert.AreEqual(model.Types["ReplicateTest.CustomType"]?.Type, typeof(CustomType));
            Assert.AreEqual(model.Types["ReplicateTest.Nested.CustomType"]?.Type, typeof(Nested.CustomType));
        }
    }
}
