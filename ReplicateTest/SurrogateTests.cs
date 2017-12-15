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
        public ulong id { get; set; }
        [Replicate]
        public string data { get; set; }
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
    [Replicate]
    public class GenericSurrogate<T> where T : class
    {
        [Replicate]
        public ulong id;
        public static implicit operator T(GenericSurrogate<T> self)
        {
            return new GameObject() { id = self.id, data = "Surrogated" } as T;
        }
        public static implicit operator GenericSurrogate<T>(GameObject go)
        {
            return new GenericSurrogate<T>() { id = go.id };
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
        [TestMethod]
        public void GenericSurrogateTest1()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GenericSurrogate<GameObject>));
            var result = Util.SerializeDeserialize(new GameObject() { id = 123, data = "faff" }, model);
            Assert.AreEqual((ulong)123, result.id);
            Assert.AreEqual("Surrogated", result.data);
        }
    }
}
