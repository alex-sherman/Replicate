using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using Replicate.MetaData;

namespace ReplicateTest
{
    public class ReplicatedType2
    {
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
        static ReplicationData typeData;
        [ClassInitialize]
        public static void InitTypes(TestContext testContext)
        {
            typeData = ReplicationModel.Default.Add(typeof(ReplicatedType));
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
    }
}
