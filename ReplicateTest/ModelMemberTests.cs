using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.MetaTyping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateTest
{
    [TestFixture]
    public class ModelMemberTests
    {
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class AutoAddType
        {
            public string Field;
            public string Property { get; set; }
        }
        [ReplicateType(AutoMembers = AutoAdd.None)]
        public class NoAutoAddType
        {
            public string Field;
            [Replicate]
            public string Explicit;
            public string Property { get; set; }
        }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class InheritedNoAutoAddType : NoAutoAddType
        {
            public string ChildAutoField;
        }

        [Test]
        public void MembersAutoAdded()
        {
            var model = new ReplicationModel();
            var typeData = model[typeof(AutoAddType)];
            Assert.NotNull(typeData);
            Assert.NotNull(typeData["Field"]);
            Assert.NotNull(typeData["Property"]);
        }
        [Test]
        public void MembersNotAutoAdded()
        {
            var model = new ReplicationModel();
            var typeData = model[typeof(NoAutoAddType)];
            Assert.NotNull(typeData);
            Assert.IsNull(typeData["Field"]);
            Assert.IsNull(typeData["Property"]);
            Assert.NotNull(typeData["Explicit"]);
        }
        [Test]
        public void InheritedMembersNotAutoAdded()
        {
            var model = new ReplicationModel(false);
            model.Add(typeof(InheritedNoAutoAddType));
            var typeData = model[typeof(InheritedNoAutoAddType)];
            Assert.NotNull(typeData);
            Assert.IsNull(typeData["Field"]);
            Assert.IsNull(typeData["Property"]);
            Assert.NotNull(typeData["Explicit"]);
            Assert.NotNull(typeData["ChildAutoField"]);
        }
    }
}
