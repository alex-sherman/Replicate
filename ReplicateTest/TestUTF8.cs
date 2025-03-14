﻿using NUnit.Framework;
using Replicate.Serialization;
using System.IO;

namespace ReplicateTest {
    [TestFixture]
    public class TestUTF8 {
        [Test]
        public void TestVariableLength() {
            var stream = new MemoryStream();
            stream.WriteString("ᚢᚱ᛫ᚠ😈");
            stream.Position = 0;
            Assert.AreEqual('ᚢ', stream.ReadCharOne());
            Assert.AreEqual('ᚱ', stream.ReadCharOne());
            Assert.AreEqual('᛫', stream.ReadCharOne());
            Assert.AreEqual('ᚠ', stream.ReadCharOne());
            Assert.AreEqual("😈", stream.ReadChars(1));
        }
    }
}
