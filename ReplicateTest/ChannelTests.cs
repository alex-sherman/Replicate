using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using System.Threading.Tasks;

namespace ReplicateTest {
    [TestFixture]
    [ReplicateType]
    public class ChannelTests {
        [ReplicateRPC]
        public static Task<string> TestMethod(string input) {
            return Task.FromResult(input + " TEST");
        }
        [Test]
        public void LambdaRequestString() {
            PassThroughChannel channel = new PassThroughChannel();
            channel.SetSerializer(new NonSerializer(new ReplicationModel()));
            channel.PointB.Server.Respond<string, string>(TestMethod);
            var variable = "herp";
            var result = channel.PointA.Request(() => TestMethod(variable + " derp")).Result;
            Assert.AreEqual("herp derp TEST", result);
        }
    }
}
