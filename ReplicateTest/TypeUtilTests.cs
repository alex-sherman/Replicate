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
    }
}
