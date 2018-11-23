using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using Replicate.MetaData;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace ReplicateTest
{
    [Replicate]
    public class ReplicatedType2
    {
        [Replicate]
        public float field3;
    }
    [Replicate]
    public class ReplicatedType
    {
        [Replicate]
        public int field1;
        [Replicate]
        public string field2;
        [Replicate]
        [ReplicatePolicy(AsReference = true)]
        public ReplicatedType2 child1;
    }

    [TestClass]
    public class ReplicationTests
    {
        static TypeAccessor typeAccessor;
        [ClassInitialize]
        public static void InitTypes(TestContext testContext)
        {
            typeAccessor = ReplicationModel.Default.GetTypeAccessor(typeof(ReplicatedType));
        }
        [TestMethod]
        public void TypeDataTest()
        {
            Assert.AreEqual(ReplicationModel.Default[typeof(ReplicatedType)].ReplicatedMembers[0].Name, "field1");
        }
        [Replicate]
        public struct SimpleMessage
        {
            [Replicate]
            public float time;
            [Replicate]
            public string faff;
        }
        [TestMethod, Timeout(100)]
        public void TestSendRecv()
        {
            var testMessage = new SimpleMessage()
            {
                time = 10,
                faff = "FAFF"
            };
            var cs = Util.MakeClientServer();
            bool called = false;
            cs.client.Channel.Subscribe("herp", (message) =>
            {
                called = true;
                Assert.AreEqual(message.Request, testMessage);
                return null;
            });
            cs.server.Channel.Publish("herp", testMessage).Wait();
            Assert.IsTrue(called);
        }
        [TestMethod]
        public void GetSetTest()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp"
            };
            Assert.AreEqual(typeAccessor.MemberAccessors[1].GetValue(replicated), "herpderp");
            Assert.AreEqual(typeAccessor.MemberAccessors[0].GetValue(replicated), 3);
            typeAccessor.MemberAccessors[1].SetValue(replicated, "FAFF");
            Assert.AreEqual(typeAccessor.MemberAccessors[1].GetValue(replicated), "FAFF");
        }
        [TestMethod]
        public void RegisterObj()
        {
            ReplicatedType replicated = new ReplicatedType();
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated);
            Assert.IsInstanceOfType(cs.client.IDLookup.Values.First().replicated, typeof(ReplicatedType));
        }
        [TestMethod]
        public void ReplicateObj()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp"
            };
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated);
            cs.server.Replicate(replicated);
            cs.server.RegisterObject(replicated).Wait();
            Assert.IsInstanceOfType(cs.client.IDLookup.Values.First().replicated, typeof(ReplicatedType));
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            Assert.AreEqual(replicated.field1, clientValue.field1);
            Assert.AreEqual(replicated.field2, clientValue.field2);
        }
        [TestMethod]
        public void ReplicateObjReference()
        {
            ReplicatedType2 child = new ReplicatedType2()
            {
                field3 = .9f
            };
            ReplicatedType replicated1 = new ReplicatedType()
            {
                child1 = child
            };
            ReplicatedType replicated2 = new ReplicatedType()
            {
                child1 = child
            };
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated1);
            cs.server.RegisterObject(replicated2);
            cs.server.RegisterObject(child);
            cs.server.Replicate(replicated1);
            cs.server.Replicate(replicated2).Wait();
            Assert.IsInstanceOfType(cs.client.IDLookup.Values.First().replicated, typeof(ReplicatedType));
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            ReplicatedType clientValue2 = (ReplicatedType)cs.client.IDLookup.Values.Skip(1).First().replicated;
            Assert.AreEqual(clientValue.child1, clientValue2.child1);
        }
        [TestMethod]
        public void ReplicateDictionary()
        {
            Dictionary<string, int> faff = new Dictionary<string, int>
            {
                ["herp"] = 3
            };
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(faff);
            cs.server.Replicate(faff).Wait();
            Assert.IsInstanceOfType(cs.client.IDLookup.Values.First().replicated, typeof(Dictionary<string, int>));
            Dictionary<string, int> clientValue = (Dictionary<string, int>)cs.client.IDLookup.Values.First().replicated;
            Assert.AreEqual("herp", clientValue.Keys.First());
            Assert.AreEqual(3, clientValue["herp"]);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RegisterSurrogatedType()
        {
            TypedValue v = new TypedValue("faff");
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(v);
        }
        class Unknown { }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RegisterUnknownType()
        {
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(new Unknown());
        }
    }
}
