using NUnit.Framework;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class JSONRepGraphTypeless
    {
        [TestCase(null, "null")]
        [TestCase(0, "0")]
        [TestCase(1, "1")]
        [TestCase(0.5, "0.5")]
        [TestCase("", "\"\"")]
        [TestCase("\n\t\\\n", "\"\\n\\t\\\\\\n\"")]
        [TestCase("😈", "\"😈\"")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void TestDeserialize(object obj, string serialized)
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<IRepNode>(serialized);
            Assert.AreEqual(obj, output.Value);
        }
        [TestCase(null, "null")]
        [TestCase(0, "0")]
        [TestCase(1, "1")]
        [TestCase(0.5, "0.5")]
        [TestCase("", "\"\"")]
        [TestCase("\n\t\\\r\n", "\"\\n\\t\\\\\\n\"")]
        [TestCase("😈", "\"😈\"")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void TestSerialize(object obj, string serialized)
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Serialize(obj?.GetType() ?? typeof(string), obj);
            Assert.AreEqual(serialized, output);
        }
        [Test]
        public void TestDeserializeEmptyCollection()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<IRepNode>("[]");
            Assert.IsTrue(output.MarshalMethod == MarshalMethod.Collection);
            Assert.IsFalse(output.AsCollection.Any());
        }
        [Test]
        public void TestDeserializeStringCollection()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<IRepNode>("[\"faff\"]");
            Assert.AreEqual(MarshalMethod.Collection, output.MarshalMethod);
            Assert.IsTrue(output.AsCollection.Any());
            Assert.AreEqual("faff", output.AsCollection.First().Value);
        }
        [Test]
        public void TestDeserializeDictionary()
        {
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<IRepNode>("{\"faff\": 123}");
            Assert.AreEqual(MarshalMethod.Object, output.MarshalMethod);
            Assert.AreEqual(123, output.AsObject["faff"].Value);
        }
        [Test]
        public void DeserializeCardsResponse()
        {
            var s = "{\"Cards\": {\"All\": [{\"Id\": 1, \"Index\": 0, \"Title\": \"Get MD Working\", \"Description\": \"Some Markdown\\n=====\\n\\n```csharp\\n var herp = \\\"derp\\\";\\n```\", \"ColumnId\": 2, \"TagIds\": [1, 2, 3, 4, 5]}, {\"Id\": 2, \"Index\": 1, \"Title\": \"Make sure UTF8 works \uD83D\uDE11\", \"Description\": \"\uD83D\uDE08\uD83D\uDE08\uD83D\uDE08\uD83D\uDE08\uD83D\uDE08\uD83D\uDE08\", \"ColumnId\": 1, \"TagIds\": [1]}, {\"Id\": 3, \"Index\": 2, \"Title\": \"Some Bug\", \"Description\": \"There was a bug\", \"ColumnId\": 2, \"TagIds\": [2, 4]}, {\"Id\": 4, \"Index\": 3, \"Title\": \"Fixed Bug\", \"Description\": \"There was a bug\", \"ColumnId\": 3, \"TagIds\": [4]}]}, \"Columns\": [{\"Id\": 1, \"Index\": 0, \"Title\": \"To Do\"}, {\"Id\": 2, \"Index\": 1, \"Title\": \"In Progress\"}, {\"Id\": 3, \"Index\": 2, \"Title\": \"Done\"}], \"Tags\": [{\"Id\": 1, \"Name\": \"Story\", \"Color\": \"#001f3f\", \"Character\": null}, {\"Id\": 2, \"Name\": \"Dev Task\", \"Color\": \"#2ECC40\", \"Character\": null}, {\"Id\": 3, \"Name\": \"Business Boiz\", \"Color\": \"#FF851B\", \"Character\": null}, {\"Id\": 4, \"Name\": \"Bug\", \"Color\": null, \"Character\": \"bug\"}, {\"Id\": 5, \"Name\": \"Star\", \"Color\": \"#F012BE\", \"Character\": \"star\"}]}";
            var ser = new JSONGraphSerializer(new ReplicationModel());
            var output = ser.Deserialize<IRepNode>(s);
        }
    }
}
