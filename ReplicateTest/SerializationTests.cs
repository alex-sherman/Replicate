using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate;
using ProtoBuf;
using System.Diagnostics;
using Replicate.MetaData;
using System.Reflection;

namespace ReplicateTest
{
    [TestClass]
    public class SerializationTests
    {
        [Replicate]
        struct SimpleMessage
        {
            [Replicate]
            public float time;
            [Replicate]
            public string faff;
        }
        [TestMethod]
        public void TestSendRecv()
        {
            var testMessage = new SimpleMessage()
            {
                time = 10,
                faff = "FAFF"
            };
            var cs = Util.MakeClientServer();
            bool called = false;
            cs.client.RegisterHandler<SimpleMessage>(0, (message) =>
            {
                called = true;
                Assert.AreEqual(message, testMessage);
            });
            cs.server.Send(0, testMessage);
            cs.client.Receive().Wait();
            Assert.IsTrue(called);
        }
    }
}
