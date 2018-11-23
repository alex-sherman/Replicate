using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Replicate.Serialization;

namespace ReplicateTest
{
    [TestClass]
    public class TestUTF8
    {
        [TestMethod]
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
