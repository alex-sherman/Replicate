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
        public class NoAttributeType
        {
            public string Field;
        }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class InheritedNoAttributeType : NoAttributeType { }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class StaticAndInstanceMembers
        {
            public static string StaticField;
            public string Field;
        }
        [ReplicateType(AutoMembers = AutoAdd.AllPublic)]
        public class PrivateSetter
        {
            public string Private { get; private set; }
            public string Protected { get; protected set; }
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
            var typeData = model.Add(typeof(InheritedNoAutoAddType));
            Assert.NotNull(typeData);
            Assert.IsNull(typeData["Field"]);
            Assert.IsNull(typeData["Property"]);
            Assert.NotNull(typeData["Explicit"]);
            Assert.NotNull(typeData["ChildAutoField"]);
        }
        [Test]
        public void InheritedMembersWithoutAttributeUseChildAttribute()
        {
            var model = new ReplicationModel(false);
            var typeData = model.Add(typeof(InheritedNoAttributeType));
            Assert.NotNull(typeData);
            Assert.NotNull(typeData["Field"]);
        }
        [Test]
        public void StaticMembersAreSeparate()
        {
            var model = new ReplicationModel(false);
            var typeData = model.Add(typeof(StaticAndInstanceMembers));
            Assert.NotNull(typeData);
            Assert.NotNull(typeData["Field"]);
            Assert.Null(typeData["StaticField"]);
            Assert.NotNull(typeData.StaticMembers["StaticField"]);
        }
        [Test]
        public void PrivateSetterWorks()
        {
            var model = new ReplicationModel(false);
            var typeData = model.Add(typeof(PrivateSetter));
            var accessor = model.GetTypeAccessor(typeof(PrivateSetter));
            Assert.NotNull(accessor);
            Assert.NotNull(accessor["Private"]);
            var obj = new PrivateSetter();
            accessor["Private"].SetValue(obj, "derp");
            Assert.AreEqual(obj.Private, "derp");
            accessor["Protected"].SetValue(obj, "herp");
            Assert.AreEqual(obj.Protected, "herp");
        }
    }
}
