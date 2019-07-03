using System;
using Replicate.MetaData;
using Replicate;
using NUnit.Framework;

namespace ReplicateTest
{
    [ReplicateType]
    public class GameObject
    {
        [Replicate]
        public ulong id { get; set; }
        [Replicate]
        public string data { get; set; }
    }
    [ReplicateType]
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
    [ReplicateType]
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
    [TestFixture]
    public class SurrogateTests
    {

        [Test]
        public void CircularSurrogateTest()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GameObjectSurrogate));
            Assert.Throws<InvalidOperationException>(() =>
                model[typeof(GameObjectSurrogate)].SetSurrogate(typeof(GameObject)));
        }
        [Test]
        public void SurrogateTest1()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GameObjectSurrogate));
            var result = BinarySerializerUtil.SerializeDeserialize(new GameObject() { id = 123, data = "faff" }, model);
            Assert.AreEqual((ulong)124, result.id);
            Assert.AreEqual("Surrogated", result.data);
        }
        [Test]
        public void GenericSurrogateTest1()
        {
            var model = new ReplicationModel();
            model[typeof(GameObject)].SetSurrogate(typeof(GenericSurrogate<GameObject>));
            var result = BinarySerializerUtil.SerializeDeserialize(new GameObject() { id = 123, data = "faff" }, model);
            Assert.AreEqual((ulong)123, result.id);
            Assert.AreEqual("Surrogated", result.data);
        }
    }
}
