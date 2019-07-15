using NUnit.Framework;
using Replicate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [ReplicateType]
    public class TypeA
    {
        public string A;
        public int B;
        public string C;
    }
    [ReplicateType]
    public class TypeB
    {
        public string A;
        public int B;
        public int C;
    }

    [TestFixture]
    class TypeUtilTests
    {
        [Test]
        public void UpdateMembersSameType()
        {
            var A = new TypeA() { A = "herp", B = 0xfaff };
            var B = new TypeA();
            TypeUtil.CopyFrom(B, A);
            Assert.AreEqual("herp", B.A);
            Assert.AreEqual(0xfaff, B.B);
        }
        [Test]
        public void UpdateMembersDifferentType()
        {
            var A = new TypeA() { A = "herp", B = 0xfaff, C = "uhoh" };
            var B = new TypeB();
            TypeUtil.CopyFrom(B, A);
            Assert.AreEqual("herp", B.A);
            Assert.AreEqual(0xfaff, B.B);
        }
        [Test]
        public void IsSameGeneric()
        {
            Assert.IsTrue(typeof(List<>).IsSameGeneric(typeof(List<>)));
            Assert.IsTrue(typeof(List<string>).IsSameGeneric(typeof(List<>)));
            Assert.Throws<InvalidOperationException>(() => typeof(List<>).IsSameGeneric(typeof(List<string>)));
        }
        [Test]
        public void ImplementsInterface()
        {
            Assert.IsTrue(typeof(List<>).Implements(typeof(IEnumerable<>)));
            Assert.IsTrue(typeof(List<string>).Implements(typeof(IEnumerable<>)));
            Assert.IsTrue(typeof(List<string>).Implements(typeof(IEnumerable<string>)));
        }
    }
}
