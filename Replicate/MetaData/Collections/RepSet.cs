﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepSet<T> : IEnumerable<KeyValuePair<RepKey, T>> where T : class
    {
        readonly Dictionary<string, KeyValuePair<RepKey, T>> stringLookup = new Dictionary<string, KeyValuePair<RepKey, T>>();
        readonly List<KeyValuePair<RepKey, T>?> indexLookup = new List<KeyValuePair<RepKey, T>?>();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<RepKey, T>> GetEnumerator() => indexLookup.Where(v => v.HasValue).Select(v => v.Value).GetEnumerator();
        public IEnumerable<T> Values => indexLookup.Where(v => v.HasValue).Select(v => v.Value.Value);
        public IEnumerable<RepKey> Keys => indexLookup.Where(v => v.HasValue).Select(v => v.Value.Key);
        public int Count => indexLookup.Count;
        public void Clear() { stringLookup.Clear(); indexLookup.Clear(); }
        public T this[RepKey key]
        {
            get => key.Index.HasValue
                   ? indexLookup[key.Index.Value]?.Value
                   : stringLookup.TryGetValue(key.Name, out var member) ? member.Value : null;
            set
            {
                if (string.IsNullOrEmpty(key.Name))
                    throw new InvalidOperationException("Key must have a name");
                var kvp = new KeyValuePair<RepKey, T>(key, value);
                stringLookup[key.Name] = kvp;
                if (key.Index.HasValue)
                {
                    var index = key.Index.Value;
                    if (index >= indexLookup.Count)
                        indexLookup.AddRange(Enumerable.Range(0, index - indexLookup.Count + 1).Select(i => (KeyValuePair<RepKey, T>?)null));
                    indexLookup[index] = kvp;
                }
            }
        }
        public RepKey Add(string name, T value)
        {
            var key = new RepKey(indexLookup.Count, name);
            this[key] = value;
            return key;
        }
        public void AddAlias(string name, T value)
        {
            this[name] = value;
        }

        public bool ContainsKey(RepKey key) => key.Index.HasValue
                ? indexLookup.Count > key.Index.Value && indexLookup[key.Index.Value] != null
                : stringLookup.ContainsKey(key.Name);

        public RepKey GetKey(RepKey key) => key.Index.HasValue
                   ? indexLookup[key.Index.Value]?.Key ?? throw new ArgumentOutOfRangeException()
                   : stringLookup.TryGetValue(key.Name, out var member) ? member.Key : throw new ArgumentOutOfRangeException();

        public bool TryGetValue(RepKey key, out T value)
        {
            value = default;
            if (!ContainsKey(key)) return false;
            value = this[key];
            return true;
        }
    }
    public static class RepSetExtensions
    {
        public static RepSet<T> ToRepSet<T>(this IEnumerable<KeyValuePair<RepKey, T>> enumerable) where T : class
        {
            var output = new RepSet<T>();
            foreach (var kvp in enumerable)
            {
                output[kvp.Key] = kvp.Value;
            }
            return output;
        }
        public static RepSet<U> ToRepSet<T, U>(this IEnumerable<KeyValuePair<RepKey, T>> enumerable, Func<T, U> conversion) where T : class where U : class
        {
            var output = new RepSet<U>();
            foreach (var kvp in enumerable)
            {
                output[kvp.Key] = conversion(kvp.Value);
            }
            return output;
        }
    }
}
