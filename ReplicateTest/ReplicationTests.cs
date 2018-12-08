using System;
using Replicate;
using Replicate.MetaData;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;

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
        [ReplicatePolicy(AsReference = true)]
        public ReplicatedType2 child1;
    }

    [TestFixture]
    public class ReplicationTests
    {
        static TypeAccessor typeAccessor;
        [OneTimeSetUp]
        public static void InitTypes()
        {
            typeAccessor = ReplicationModel.Default.GetTypeAccessor(typeof(ReplicatedType));
        }
        [Test]
        public void TypeDataTest()
        {
            Assert.AreEqual(ReplicationModel.Default[typeof(ReplicatedType)].ReplicatedMembers[0].Name, "field1");
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
            var members = ReplicationModel.Default[typeof(IgnoredFields)].ReplicatedMembers;
            Assert.AreEqual(1, members.Count);
            Assert.AreEqual("exist", members[0].Name);
        }
        [ReplicateType]
        public struct SimpleMessage
        {
            public float time;
            public string faff;
        }
        [Test, Timeout(100)]
        public void TestSendRecv()
        {
            var testMessage = new SimpleMessage()
            {
                time = 10,
                faff = "FAFF"
            };
            var cs = Util.MakeClientServer();
            bool called = false;
            var method = new Func<SimpleMessage, Task<bool>>((message) =>
            {
                called = true;
                Assert.AreEqual(message, testMessage);
                return Task.FromResult(false);
            });
            cs.client.Channel.Respond(method);
            cs.server.Channel.Request(method.Method, testMessage).Await();
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
            Assert.AreEqual(typeAccessor.MemberAccessors[1].GetValue(replicated), "herpderp");
            Assert.AreEqual(typeAccessor.MemberAccessors[0].GetValue(replicated), 3);
            typeAccessor.MemberAccessors[1].SetValue(replicated, "FAFF");
            Assert.AreEqual(typeAccessor.MemberAccessors[1].GetValue(replicated), "FAFF");
        }
        [Test]
        public void RegisterObj()
        {
            ReplicatedType replicated = new ReplicatedType();
            var cs = Util.MakeClientServer();
            cs.server.RegisterObject(replicated);
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
        }
        [Test]
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
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            Assert.AreEqual(replicated.field1, clientValue.field1);
            Assert.AreEqual(replicated.field2, clientValue.field2);
        }
        [Test]
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
            Assert.IsInstanceOf<ReplicatedType>(cs.client.IDLookup.Values.First().replicated);
            ReplicatedType clientValue = (ReplicatedType)cs.client.IDLookup.Values.First().replicated;
            ReplicatedType clientValue2 = (ReplicatedType)cs.client.IDLookup.Values.Skip(1).First().replicated;
            Assert.AreEqual(clientValue.child1, clientValue2.child1);
        }
        // Not implemented any more, but maybe again in the future?
        //[Test]
        //public void ReplicateDictionary()
        //{
        //    Dictionary<string, int> faff = new Dictionary<string, int>
        //    {
        //        ["herp"] = 3
        //    };
        //    var cs = Util.MakeClientServer();
        //    cs.server.RegisterObject(faff);
        //    cs.server.Replicate(faff).Wait();
        //    Assert.IsInstanceOfType(cs.client.IDLookup.Values.First().replicated, typeof(Dictionary<string, int>));
        //    Dictionary<string, int> clientValue = (Dictionary<string, int>)cs.client.IDLookup.Values.First().replicated;
        //    Assert.AreEqual("herp", clientValue.Keys.First());
        //    Assert.AreEqual(3, clientValue["herp"]);
        //}
        [ReplicateType]
        public class ClassSurrogate { }
        [ReplicateType(SurrogateType = typeof(ClassSurrogate))]
        public class ClassWithSurrogate { }
        [Test]
        public void RegisterSurrogatedType()
        {
            var v = new ClassWithSurrogate();
            var cs = Util.MakeClientServer();
            Assert.Throws<InvalidOperationException>(() => cs.server.RegisterObject(v));
        }
        class Unknown { }
        [Test]
        public void RegisterUnknownType()
        {
            var cs = Util.MakeClientServer();
            Assert.Throws<InvalidOperationException>(() => cs.server.RegisterObject(new Unknown()));
        }
    }
}
