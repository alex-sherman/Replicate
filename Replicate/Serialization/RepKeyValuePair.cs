using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization {
    [ReplicateType]
    public struct RepKeyValuePair<TKey, TValue> {
        public TKey Key;
        public TValue Value;
        public static KeyValuePair<TKey, TValue> Convert(RepKeyValuePair<TKey, TValue> self)
            => new KeyValuePair<TKey, TValue>(self.Key, self.Value);
        public static RepKeyValuePair<TKey, TValue> Convert(KeyValuePair<TKey, TValue> other)
            => new RepKeyValuePair<TKey, TValue>() { Key = other.Key, Value = other.Value };
    }
}
