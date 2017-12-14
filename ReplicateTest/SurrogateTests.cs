using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Replicate.MetaData;
using Replicate;

namespace ReplicateTest
{
    [Replicate]
    public class GameObject
    {
        [Replicate]
        public ulong id;
        [Replicate]
        public string data;
    }
    [Replicate]
    public class GameObjectSurrogate
    {
        [Replicate]
        public ulong id;
        public static implicit operator GameObject(GameObjectSurrogate self)
        {
            return new GameObject() { id = self.id + 1, data = "Surrogated" };
        }
        public static implicit operator GameObjectSurrogate(GameObject go)
        {
            return new GameObjectSurrogate() { id = go.id };
        }
    }
    [TestClass]
    public class SurrogateTests
    {

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CircularSurrogateTest()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GameObjectSurrogate));
            model[typeof(GameObjectSurrogate)].SetSurrogate(typeof(GameObject));
        }
        [TestMethod]
        public void SurrogateTest1()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GameObjectSurrogate));
            var result = Util.SerializeDeserialize(new GameObject() { id = 123, data = "faff" }, model);
            Assert.AreEqual((ulong)124, result.id);
            Assert.AreEqual("Surrogated", result.data);
        }
    }
}
