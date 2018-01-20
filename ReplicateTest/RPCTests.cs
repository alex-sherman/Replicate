using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate.Interfaces;
using System.Linq;
using Replicate;

namespace ReplicateTest
{
    [TestClass]
    public class RPCTests
    {
        public interface ITestInterface
        {
            int Herp(string faff);
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
            public bool DerpCalled = false;
            public string HerpValue = null;
            public void Derp()
            {
                DerpCalled = true;
            }

            public int Herp(string faff)
            {
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
        public void CreateRPCProxyTest1()
        {
            var cs = Util.MakeClientServer();
            var target = new TestTarget();
            cs.server.RegisterObject(target);
            cs.client.PumpMessages();
            var clientTarget = cs.client.objectLookup.Values.First().replicated as TestTarget;
            var proxy = cs.server.CreateProxy<ITestInterface>(target);
            proxy.Derp();
            Assert.IsTrue(clientTarget.DerpCalled);
        }
    }
}
