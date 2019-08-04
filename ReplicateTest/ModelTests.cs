using NUnit.Framework;
using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void InvalidSetKey()
        {
            var set = new RepSet<string>();
            Assert.Throws<InvalidOperationException>(() => set["derp"] = null);
        }
        [Test]
        public void ValidSetKeyRetrieves()
        {
            var set = new RepSet<string>();
            set[new RepKey(0, "derp")] = "derp";
            Assert.AreEqual("derp", set["derp"]);
            Assert.AreEqual("derp", set[0]);
        }
        [Test]
        public void TestSparseSet()
        {
            var set = new RepSet<string>();
            set[new RepKey(5, "derp")] = "derp";
            Assert.AreEqual("derp", set["derp"]);
            Assert.AreEqual(null, set[0]);
            Assert.AreEqual(null, set[1]);
            Assert.AreEqual(null, set[2]);
            Assert.AreEqual(null, set[3]);
            Assert.AreEqual(null, set[4]);
            Assert.AreEqual("derp", set[5]);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var derp = set[6]; });
        }
        [Test]
        public void ToRepSet()
        {
            var set = new RepSet<string>();
            set[new RepKey(0, "derp")] = "derp";
            var set2 = set.Select(v => new KeyValuePair<RepKey, string>(v.Key, "herp" + v.Value)).ToRepSet();
            Assert.AreEqual("herpderp", set2[0]);
            Assert.AreEqual("herpderp", set2["derp"]);
        }
    }
}
