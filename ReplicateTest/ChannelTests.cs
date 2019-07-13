using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class ChannelTests
    {
        public static Task<string> TestMethod(string input)
        {
            return Task.FromResult(input + " TEST");
        }
        [Test]
        public void LambdaRequestString()
        {
            PassThroughChannel channel = new PassThroughChannel();
            channel.SetSerializer(new NonSerializer());
            channel.PointB.Respond<string, string>(TestMethod);
            var variable = "herp";
            var result = channel.PointA.Request(() => TestMethod(variable + " derp")).Result;
            Assert.AreEqual("herp derp TEST", result);
        }
    }
}
