namespace Replicate {
    public struct None { public static None Value { get; private set; } = new None(); }
    public class Ref<T> where T : struct {
        public Ref(T value) => Value = value;
        public T Value;
        public override string ToString() => Value.ToString();
        public static implicit operator T(Ref<T> wrapper) => wrapper.Value;
        public static implicit operator Ref<T>(T value) => new Ref<T>(value);
    }
}
