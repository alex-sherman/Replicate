using NUnit.Framework;
using Replicate.MetaTyping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class ProxyTest
    {
        public interface IDerp
        {
            int Herp(int a, int b);
        }

        public class InterceptableClass : IDerp
        {
            public int Herp(int a, int b)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ImplementTest()
        {
            var proxy = ProxyImplement.HookUp<IDerp>((method, args) =>
            {
                Assert.AreEqual(args.Length, 2);
                return (int)args[0] + (int)args[1];
            });

            Assert.AreEqual(proxy.Herp(1, 2), 3);
        }
    }
}
