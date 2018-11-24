using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Replicate.Serialization;
using NUnit.Framework;

namespace ReplicateTest
{
    [TestFixture]
    public class TestUTF8
    {
        [Test]
        public void TestVariableLength()
        {
            var stream = new MemoryStream();
            stream.WriteString("ᚢᚱ᛫ᚠ");
            stream.Position = 0;
            Assert.AreEqual('ᚢ', stream.ReadChar());
            Assert.AreEqual('ᚱ', stream.ReadChar());
            Assert.AreEqual('᛫', stream.ReadChar());
            Assert.AreEqual('ᚠ', stream.ReadChar());
        }
    }
}
