using System;
using Replicate;
using Replicate.MetaData;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using Replicate.MetaData.Policy;

namespace ReplicateTest
{
    [ReplicateType]
    public class ReplicatedType2
    {
        [Replicate]
        public float field3;
    }
    [ReplicateType]
    public class ReplicatedType
    {
        public int field1;
        public string field2;
        [AsReference]
        public ReplicatedType2 child1;
    }

    [TestFixture]
    [ReplicateType]
    public class ReplicationTests
    {
        static TypeAccessor typeAccessor;
        static ReplicationModel model = new ReplicationModel();
        [OneTimeSetUp]
        public static void InitTypes()
        {
            typeAccessor = model.GetTypeAccessor(typeof(ReplicatedType));
        }
        [Test]
        public void TypeDataTest()
        {
            Assert.AreEqual(model[typeof(ReplicatedType)][0].Name, "field1");
        }
        [ReplicateType]
        public struct IgnoredFields
        {
            public float exist;
            [ReplicateIgnore]
            public float ignored;
        }
        [Test]
        public void TestReplicateIgnore()
        {
            var members = model[typeof(IgnoredFields)].Members;
            Assert.AreEqual(1, members.Count);
            Assert.AreEqual("exist", members[0].Name);
        }
        [ReplicateType]
        public class SimpleMessage
        {
            public float time;
            public string faff;
        }
        static bool called = false;
        static SimpleMessage testMessage;
        [ReplicateRPC]
        public static Task<bool> messageMethod(SimpleMessage message)
        {
            called = true;
            Assert.AreEqual(message.time, testMessage.time);
            Assert.AreEqual(message.faff, testMessage.faff);
            return Task.FromResult(false);
        }
        [Test, Timeout(1000)]
        public void TestSendRecv()
        {
            testMessage = new SimpleMessage()
            {
                time = 10,
                faff = "FAFF"
            };
            var cs = BinarySerializerUtil.MakeClientServer();
            called = false;
            cs.client.Channel.Server.Respond<SimpleMessage, bool>(messageMethod);
            cs.server.Channel.Request(() => messageMethod(testMessage)).Await();
            Assert.IsTrue(called);
        }
        [Test]
        public void GetSetTest()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp"
            };
            Assert.AreEqual(typeAccessor[1].GetValue(replicated), "herpderp");
            Assert.AreEqual(typeAccessor[0].GetValue(replicated), 3);
            typeAccessor[1].SetValue(replicated, "FAFF");
            Assert.AreEqual(typeAccessor[1].GetValue(replicated), "FAFF");
        }
        [Test]
        public void RegisterObj()
        {
            ReplicatedType replicated = new ReplicatedType();
            var cs = BinarySerializerUtil.MakeClientServer();
            cs.server.RegisterObject(replicated);
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
        }
        // TODO: Re-enable
        //[Test]
        public void ReplicateObj()
        {
            ReplicatedType replicated = new ReplicatedType()
            {
                field1 = 3,
                field2 = "herpderp"
            };
            var cs = BinarySerializerUtil.MakeClientServer();
            cs.server.RegisterObject(replicated);
            cs.server.Replicate(replicated).Wait();
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            Assert.AreEqual(replicated.field1, clientValue.field1);
            Assert.AreEqual(replicated.field2, clientValue.field2);
        }
        // TODO: Re-enable
        //[Test]
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
            var cs = BinarySerializerUtil.MakeClientServer();
            cs.server.RegisterObject(replicated1);
            cs.server.RegisterObject(replicated2);
            cs.server.RegisterObject(child);
            cs.server.Replicate(replicated1);
            cs.server.Replicate(replicated2).Wait();
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            ReplicatedType clientValue2 = (ReplicatedType)cs.client.IDLookup.Values.Skip(1).First().replicated;
            Assert.NotNull(clientValue.child1);
            Assert.AreEqual(clientValue.child1, clientValue2.child1);
        }
        // TODO: Re-enable
        //[Test]
        public void ReplicateDictionary()
        {
            Dictionary<string, int> faff = new Dictionary<string, int>
            {
                ["herp"] = 3
            };
            var cs = BinarySerializerUtil.MakeClientServer();
            cs.server.RegisterObject(faff);
            cs.server.Replicate(faff).Wait();
            Assert.IsInstanceOf(typeof(Dictionary<string, int>), cs.client.IDLookup.Values.First().replicated);
            Dictionary<string, int> clientValue = (Dictionary<string, int>)cs.client.IDLookup.Values.First().replicated;
            Assert.AreEqual("herp", clientValue.Keys.First());
            Assert.AreEqual(3, clientValue["herp"]);
        }
        [ReplicateType]
        public class ClassSurrogate { }
        [ReplicateType(SurrogateType = typeof(ClassSurrogate))]
        public class ClassWithSurrogate { }
        [Test]
        public void RegisterSurrogatedType()
        {
            var v = new ClassWithSurrogate();
            var cs = BinarySerializerUtil.MakeClientServer();
            Assert.Throws<InvalidOperationException>(() => cs.server.RegisterObject(v));
        }
        class Unknown { }
        [Test]
        public void RegisterUnknownType()
        {
            var cs = BinarySerializerUtil.MakeClientServer();
            Assert.Throws<InvalidOperationException>(() => cs.server.RegisterObject(new Unknown()));
        }
    }
}
