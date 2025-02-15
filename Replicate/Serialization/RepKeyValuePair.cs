using System.Collections.Generic;

namespace Replicate.Serialization {
    [ReplicateType]
    public struct RepKeyValuePair<TKey, TValue> {
        [Replicate(1)]
        public TKey Key;
        [Replicate(2)]
        public TValue Value;
        public static implicit operator KeyValuePair<TKey, TValue>(RepKeyValuePair<TKey, TValue> self)
            => new KeyValuePair<TKey, TValue>(self.Key, self.Value);
        public static implicit operator RepKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> other)
            => new RepKeyValuePair<TKey, TValue>() { Key = other.Key, Value = other.Value };
    }
}
