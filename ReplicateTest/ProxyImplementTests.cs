using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate.Interfaces;

namespace ReplicateTest
{
    [TestClass]
    public class ProxyImplementTests
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
        [TestMethod]
        public void ProxyImplementTest1()
        {
            ITestInterface test = ProxyImplement.HookUp<ITestInterface>(new TestImplementor());
            test.Derp();
            var d = test.Herp("faff");
        }
    }
}
