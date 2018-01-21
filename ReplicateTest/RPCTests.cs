using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate.Interfaces;
using System.Linq;
using Replicate;
using static ReplicateTest.Util;

namespace ReplicateTest
{
    [TestClass]
    public class RPCTests
    {
        [Replicate]
        public interface ITestInterface
        {
            int Herp(string faff);
            void Derp();
        }
        [Replicate]
        public interface ITestInterface<T>
        {
            int Herp(T faff);
            void Derp();
        }
        public class TestImplementor : IImplementor
        {
            MethodInfo derp = typeof(ITestInterface).GetMethod("Derp");
            MethodInfo herp = typeof(ITestInterface).GetMethod("Herp");
            public object Intercept(MethodInfo method, object[] args)
            {
                Assert.IsTrue(method == derp || method == herp);
                if (method == herp)
                    return ((string)args[0]).Length;
                return null;
            }
        }
        [Replicate]
        public class TestTarget : ITestInterface
        {
            public ITestInterface RPC;
            public bool DerpCalled = false;
            public string HerpValue = null;
            public void Derp()
            {
                DerpCalled = true;
            }

            public int Herp(string faff)
            {
                if (!ReplicateContext.IsInRPC)
                    RPC.Herp(faff);
                HerpValue = faff;
                return faff.Length;
            }
        }
        [TestMethod]
        public void ProxyImplementTest1()
        {
            ITestInterface test = ProxyImplement.HookUp<ITestInterface>(new TestImplementor());
            test.Derp();
            var d = test.Herp("faff");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ProxyOnUnregisteredObject()
        {
            Util.MakeClientServer().server.CreateProxy(new TestTarget());
        }
        [TestMethod]
        public void ReplicatedInterface1()
        {
            var repint = new ReplicatedInterface(typeof(ITestInterface));
            var methinfo = typeof(ITestInterface).GetMethod("Derp");
            byte methID = repint.GetMethodID(methinfo);
            Assert.AreEqual(methinfo, repint.GetMethodFromID(methID));
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReplicatedInterface2()
        {
            var repInt = new ReplicatedInterface(typeof(ITestInterface<>));
        }
        ClientServer rpcSetup(out TestTarget serverTarget, out TestTarget clientTarget)
        {
            var cs = Util.MakeClientServer();
            serverTarget = new TestTarget();
            cs.server.RegisterObject(serverTarget);
            cs.client.PumpMessages();
            clientTarget = cs.client.objectLookup.Values.First().replicated as TestTarget;
            serverTarget.RPC = cs.server.CreateProxy<ITestInterface>(serverTarget);
            clientTarget.RPC = cs.client.CreateProxy<ITestInterface>(clientTarget);
            return cs;
        }
        [TestMethod]
        public void RPCProxyTest1()
        {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.RPC.Derp();
            cs.client.PumpMessages();
            Assert.IsTrue(clientTarget.DerpCalled);
        }
        [TestMethod]
        public void RPCProxyTest2()
        {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.Herp("derp");
            cs.client.PumpMessages();
            Assert.AreEqual("derp", clientTarget.HerpValue);
            Assert.IsFalse(cs.server.Channel.MessageQueue.Any());
        }
    }
}
