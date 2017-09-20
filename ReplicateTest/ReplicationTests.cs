using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using Replicate.MetaData;
using System.Linq;

namespace ReplicateTest
{
    public class ReplicatedType2
    {
        [Replicate]
        public float field3;
    }
    public class ReplicatedType
    {
        [Replicate]
        public int field1;
        [Replicate]
        public string field2;
        [Replicate]
        public ReplicatedType2 child1;
    }

    [TestClass]
    public class ReplicationTests
    {
        static TypeData typeData;
        [ClassInitialize]
        public static void InitTypes(TestContext testContext)
        {
            typeData = ReplicationModel.Default.Add(typeof(ReplicatedType));
            ReplicationModel.Default.Add(typeof(ReplicatedType2));
            ReplicationModel.Default.Compile();
        }
        [TestMethod]
        public void TypeDataTest()
        {
            Assert.AreEqual(ReplicationModel.Default[typeof(ReplicatedType)].ReplicatedMembers[0].Name, "field1");
        }
        [TestMethod]
        public void GetSetTest()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp"
            };
            Assert.AreEqual(typeData.ReplicatedMembers[0].GetValue(replicated), 3);
            Assert.AreEqual(typeData.ReplicatedMembers[1].GetValue(replicated), "herpderp");
            typeData.ReplicatedMembers[1].SetValue(replicated, "FAFF");
            Assert.AreEqual(typeData.ReplicatedMembers[1].GetValue(replicated), "FAFF");
        }
        [TestMethod]
        public void RegisterObj()
        {
            ReplicatedType replicated = new ReplicatedType();
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated);
            Assert.IsFalse(cs.client.idLookup.Any());
            cs.client.PumpMessages();
            Assert.IsInstanceOfType(cs.client.idLookup.Values.First().replicated, typeof(ReplicatedType));
        }
        [TestMethod]
        public void ReplicateObj()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp",
                child1 = new ReplicatedType2()
                {
                    field3 = .9f
                }
            };
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated);
            cs.server.Replicate(replicated);
            Assert.IsFalse(cs.client.idLookup.Any());
            cs.client.PumpMessages();
            Assert.IsInstanceOfType(cs.client.idLookup.Values.First().replicated, typeof(ReplicatedType));
            ReplicatedType clientValue = (ReplicatedType)cs.client.idLookup.Values.First().replicated;
            Assert.AreEqual(clientValue.field1, replicated.field1);
            Assert.AreEqual(clientValue.field2, replicated.field2);
        }
    }
}
