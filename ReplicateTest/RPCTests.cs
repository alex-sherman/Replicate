﻿using System;
using System.Reflection;
using Replicate.Interfaces;
using System.Linq;
using Replicate;
using static ReplicateTest.Util;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ReplicateTest
{
    [TestFixture]
    public class RPCTests
    {
        public interface ITestInterface
        {
            int Herp(string faff);
            void Derp();
            Task<int> AsyncHerp(string faff);
            Task AsyncDerp();
        }
        public interface ITestInterface<T>
        {
            int Herp(T faff);
            void Derp();
        }
        public class TestImplementor : IImplementor
        {
            MethodInfo derp = typeof(ITestInterface).GetMethod("Derp");
            MethodInfo herp = typeof(ITestInterface).GetMethod("Herp");

            public T Intercept<T>(MethodInfo method, object[] args)
            {
                Assert.IsTrue(method == derp || method == herp);
                if (method == herp)
                    return (T)(object)((string)args[0]).Length;
                return default(T);
            }

            public Task<T> InterceptAsync<T>(MethodInfo method, object[] args)
            {
                throw new NotImplementedException();
            }

            public Task InterceptAsyncVoid(MethodInfo method, object[] args)
            {
                throw new NotImplementedException();
            }

            public void InterceptVoid(MethodInfo method, object[] args)
            {
                throw new NotImplementedException();
            }
        }
        [ReplicateType]
        public class TestTarget : ITestInterface
        {
            public ITestInterface RPC;
            public bool DerpCalled = false;
            public string HerpValue = null;
            public void Derp()
            {
                DerpCalled = true;
            }
            public async Task AsyncDerp()
            {
                await Task.Delay(100);
                Derp();
            }

            public int Herp(string faff)
            {
                if (!ReplicateContext.IsInRPC)
                    RPC.Herp(faff);
                HerpValue = faff;
                return faff.Length;
            }
            public async Task<int> AsyncHerp(string faff)
            {
                await Task.Delay(100);
                return Herp(faff);
            }
        }
        [Test]
        public void ProxyImplementTest1()
        {
            ITestInterface test = ProxyImplement.HookUp<ITestInterface>(new TestImplementor());
            Assert.AreEqual(4, test.Herp("faff"));
        }
        [Test]
        public void ProxyOnUnregisteredObject()
        {
            Assert.Throws<InvalidOperationException>(
                () => MakeClientServer().server.CreateProxy(new TestTarget()));
        }
        ClientServer rpcSetup(out TestTarget serverTarget, out TestTarget clientTarget)
        {
            var cs = MakeClientServer();
            serverTarget = new TestTarget();
            cs.server.RegisterObject(serverTarget).Wait();
            cs.server.RegisterInstanceInterface<ITestInterface>();
            cs.client.RegisterInstanceInterface<ITestInterface>();
            clientTarget = cs.client.ObjectLookup.Values.First().replicated as TestTarget;
            serverTarget.RPC = cs.server.CreateProxy<ITestInterface>(serverTarget);
            clientTarget.RPC = cs.client.CreateProxy<ITestInterface>(clientTarget);
            return cs;
        }
        [Test]
        public void RPCProxyTest1()
        {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.RPC.Derp();
            Assert.IsTrue(clientTarget.DerpCalled);
        }
        [Test]
        public void RPCProxyTest2()
        {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.Herp("derp");
            Assert.AreEqual("derp", clientTarget.HerpValue);
        }
        [Test]
        public void TestRPCTargetGetsCalled()
        {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            proxy.Derp();
            Assert.IsTrue(target.DerpCalled);
        }
        [Test]
        public void TestRPCTargetGetsCalledAsync()
        {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            proxy.AsyncDerp().Wait();
            Assert.IsTrue(target.DerpCalled);
        }
        [Test]
        public void TestRPCReturnValueGetsSent()
        {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            Assert.AreEqual(4, proxy.Herp("derp"));
        }
        [Test]
        public void TestRPCReturnValueGetsSentAsync()
        {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            Assert.AreEqual(4, proxy.AsyncHerp("derp").Result);
        }
    }
}
