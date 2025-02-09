using NUnit.Framework;
using Replicate;
using System.Collections.Generic;

namespace ReplicateTest {
    [ReplicateType]
    public class TypeA {
        public string A;
        public int B;
        public string C;
    }
    [ReplicateType]
    public class TypeB {
        public string A;
        public int B;
        public int C;
    }

    public class FunctionClass {
        public static int Derp() => 3;
        public int Herp() => 4;
    }

    [TestFixture]
    class TypeUtilTests {
        [Test]
        public void UpdateMembersSameType() {
            var A = new TypeA() { A = "herp", B = 0xfaff };
            var B = new TypeA();
            TypeUtil.CopyTo(A, B);
            Assert.AreEqual("herp", B.A);
            Assert.AreEqual(0xfaff, B.B);
        }
        [Test]
        public void UpdateMembersDifferentType() {
            var A = new TypeA() { A = "herp", B = 0xfaff, C = "uhoh" };
            var B = new TypeB();
            TypeUtil.CopyTo(A, B);
            Assert.AreEqual("herp", B.A);
            Assert.AreEqual(0xfaff, B.B);
        }
        [Test]
        public void IsSameGeneric() {
            Assert.IsTrue(typeof(List<>).IsSameGeneric(typeof(List<>)));
            Assert.IsTrue(typeof(List<string>).IsSameGeneric(typeof(List<>)));
            Assert.IsTrue(typeof(List<>).IsSameGeneric(typeof(List<string>)));
        }
        [Test]
        public void ImplementsInterface() {
            Assert.IsTrue(typeof(List<>).Implements(typeof(IEnumerable<>)));
            Assert.IsTrue(typeof(List<string>).Implements(typeof(IEnumerable<>)));
            Assert.IsTrue(typeof(List<string>).Implements(typeof(IEnumerable<string>)));
        }
        [Test]
        public void GetMethodStatic() {
            Assert.AreEqual(TypeUtil.GetMethod(() => FunctionClass.Derp())
                .Invoke(null, new object[] { }), 3);
        }
        [Test]
        public void GetMethodInstance() {
            var obj = new FunctionClass();
            Assert.AreEqual(TypeUtil.GetMethod<FunctionClass>((c) => c.Herp())
                .Invoke(obj, new object[] { }), 4);
        }
    }
}
