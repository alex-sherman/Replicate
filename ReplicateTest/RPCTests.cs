using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaTyping;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static ReplicateTest.BinarySerializerUtil;

namespace ReplicateTest {
    public class AddImplementor : IAutoAddNone, IAutoAddAllPublic {
        public bool AddModifier() => true;
        public bool IgnoreModifier() => true;
        public bool NoModifiers() => true;
    }
    [ReplicateType(AutoMethods = AutoAdd.AllPublic)]
    public interface IAutoAddAllPublic {
        bool NoModifiers();
        [ReplicateRPC]
        bool AddModifier();
        [ReplicateIgnore]
        bool IgnoreModifier();
    }
    [ReplicateType(AutoMethods = AutoAdd.None)]
    public interface IAutoAddNone {
        bool NoModifiers();
        [ReplicateRPC]
        bool AddModifier();
        [ReplicateIgnore]
        bool IgnoreModifier();
    }
    [ReplicateType]
    public interface IMultipleParameters {
        bool TwoParameters(int a, bool b);
    }
    [ReplicateType]
    public interface IDefaultParameter {
        int TwoParameters(int a, bool b = false);
    }
    public class DefaultParameter : IDefaultParameter {
        public int TwoParameters(int a, bool b = false) {
            Assert.IsFalse(b);
            return a;
        }
    }
    [TestFixture]
    public class RPCTests {
        [ReplicateType(IsInstanceRPC = true)]
        public interface ITestInterface {
            int Herp(string faff);
            void Derp();
            Task<int> AsyncHerp(string faff);
            Task AsyncDerp();
        }
        [ReplicateType]
        public interface ITestGenericInterface {
            T Generic<T>(T value);
        }
        [ReplicateType(IsInstanceRPC = true)]
        public interface ITestInterface2 {
            [ReplicateRPC]
            int Herp(string faff);
            void Derp();
        }
        public interface ITestInterface<T> {
            int Herp(T faff);
            void Derp();
        }
        public class TestImplementor : IInterceptor {
            MethodInfo derp = typeof(ITestInterface).GetMethod("Derp");
            MethodInfo herp = typeof(ITestInterface).GetMethod("Herp");

            public T Intercept<T>(MethodInfo method, object[] args) {
                if (method.Name == "Generic")
                    return (T)args[0];
                Assert.IsTrue(method == derp || method == herp);
                if (method == herp)
                    return (T)(object)((string)args[0]).Length;
                return default(T);
            }
        }
        [ReplicateType]
        public class TestTarget : ITestInterface, ITestInterface2 {
            public ITestInterface RPC;
            public bool DerpCalled = false;
            public string HerpValue = null;
            public void Derp() {
                DerpCalled = true;
            }
            public async Task AsyncDerp() {
                await Task.Yield();
                Derp();
            }
            public int Herp(string faff) {
                if (!ReplicateContext.IsInRPC)
                    RPC.Herp(faff);
                HerpValue = faff;
                return faff.Length;
            }
            public async Task<int> AsyncHerp(string faff) {
                await Task.Yield();
                return Herp(faff);
            }
            [ReplicateIgnore]
            public void Ignored() { }
        }
        [Test]
        public void ProxyImplementTest1() {
            ITestInterface test = ProxyImplement.HookUp<ITestInterface>(new TestImplementor());
            Assert.AreEqual(4, test.Herp("faff"));
        }
        [Test]
        public void ProxyImplementGeneric() {
            // TODO: Implement probably
            Assert.Throws<ReplicateError>(() => {
                ITestGenericInterface test = ProxyImplement.HookUp<ITestGenericInterface>(new TestImplementor());
            });
            //Assert.AreEqual("faff", test.Generic("faff"));
        }
        [Test]
        public void ProxyOnUnregisteredObject() {
            Assert.Throws<ReplicateError>(() => MakeClientServer().server.CreateProxy(new TestTarget()));
        }
        ClientServer rpcSetup(out TestTarget serverTarget, out TestTarget clientTarget) {
            var cs = MakeClientServer();
            serverTarget = new TestTarget();
            cs.server.RegisterObject(serverTarget).Await();
            clientTarget = cs.client.ObjectLookup.Values.First().replicated as TestTarget;
            serverTarget.RPC = cs.server.CreateProxy<ITestInterface>(serverTarget);
            clientTarget.RPC = cs.client.CreateProxy<ITestInterface>(clientTarget);
            return cs;
        }
        [Test]
        public void RPCProxyTest1() {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.RPC.Derp();
            Assert.IsTrue(clientTarget.DerpCalled);
        }
        [Test]
        public void RPCProxyTest2() {
            var cs = rpcSetup(out var serverTarget, out var clientTarget);
            serverTarget.Herp("derp");
            Assert.AreEqual("derp", clientTarget.HerpValue);
        }
        [Test]
        public void AutoAddNone() {
            var channel = new PassThroughChannel.Endpoint(new ReplicationModel());
            channel.target = channel;
            channel.Server.RegisterSingleton<IAutoAddNone>(new AddImplementor());
            var proxy = channel.CreateProxy<IAutoAddNone>();
            Assert.IsTrue(proxy.AddModifier());
            Assert.Throws<ContractNotFoundError>(() => proxy.IgnoreModifier());
            Assert.Throws<ContractNotFoundError>(() => proxy.NoModifiers());
        }
        [Test]
        public void AutoAddPublic() {
            var channel = new PassThroughChannel.Endpoint(new ReplicationModel());
            channel.target = channel;
            channel.Server.RegisterSingleton<IAutoAddAllPublic>(new AddImplementor());
            var proxy = channel.CreateProxy<IAutoAddAllPublic>();
            Assert.IsTrue(proxy.AddModifier());
            Assert.Throws<ContractNotFoundError>(() => proxy.IgnoreModifier());
            Assert.IsTrue(proxy.NoModifiers());
        }
        [Test]
        public void TestRPCTargetGetsCalled() {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.Server.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            proxy.Derp();
            Assert.IsTrue(target.DerpCalled);
        }
        [Test]
        public void TestRPCTargetGetsCalledAsync() {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.Server.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            proxy.AsyncDerp().Wait();
            Assert.IsTrue(target.DerpCalled);
        }
        [Test]
        public void TestRPCReturnValueGetsSent() {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.Server.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            Assert.AreEqual(4, proxy.Herp("derp"));
        }
        [Test]
        public void TestRPCReturnValueGetsSentAsync() {
            var cs = MakeClientServer();
            var target = new TestTarget();
            cs.server.Channel.Server.RegisterSingleton<ITestInterface>(target);
            var proxy = cs.client.CreateProxy<ITestInterface>();
            Assert.AreEqual(4, proxy.AsyncHerp("derp").Result);
        }
        [Test]
        public void MultipleParametersError() {
            var channel = new PassThroughChannel.Endpoint(new ReplicationModel());
            channel.target = channel;
            Assert.Throws<ReplicateError>(() => channel.Server.RegisterSingleton<IMultipleParameters>(null));
        }
        [Test]
        public void DefaultParametersSuccess() {
            var channel = new PassThroughChannel.Endpoint(new ReplicationModel());
            channel.target = channel;
            channel.Server.RegisterSingleton<IDefaultParameter>(new DefaultParameter());
            channel.TryGetContract(channel.Server.Model.MethodKey(typeof(IDefaultParameter).GetMethods().First()), out var contract);
            Assert.AreEqual(contract.RequestType, typeof(int));
            var proxy = channel.CreateProxy<IDefaultParameter>();
            Assert.AreEqual(5, proxy.TwoParameters(5));
        }
    }
}
