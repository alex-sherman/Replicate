using System;
using System.Collections.Generic;

namespace Replicate.Serialization {
    public static class CollectionUtil {
        public static object FillCollection(object collection, Type type, List<object> values) {
            var count = values.Count;

            // Interfaces cannot be constructed, so use a List to implement these interfaces
            if (type.IsSameGeneric(typeof(IEnumerable<>)) || type.IsSameGeneric(typeof(ICollection<>))) {
                type = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                collection = null;
            }
            var collectionType = type.GetInterface("ICollection`1");
            if (collection is Array || collection == null)
                collection = Activator.CreateInstance(type, count);
            else {
                var clearMeth = collectionType.GetMethod("Clear");
                clearMeth.Invoke(collection, new object[] { });
            }
            if (collection is Array) {
                var arr = collection as Array;
                for (int i = 0; i < count; i++)
                    arr.SetValue(values[i], i);
            } else {
                var addMeth = collectionType.GetMethod("Add");
                foreach (var value in values)
                    addMeth.Invoke(collection, new object[] { value });
            }
            return collection;
        }
        public static void AddToDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> value) {
            dict[value.Key] = value.Value;
        }
        public static void AddToCollection(object collection, object value) {
            var type = collection.GetType();
            var dictionaryType = type.GetInterface("IDictionary`2");
            if (dictionaryType != null) {
                var setMeth = typeof(CollectionUtil).GetMethod("AddToDictionary");
                if (setMeth == null) throw new ReplicateError($"Cannot deserialize repeated fields into {type.Name}");
                setMeth = setMeth.MakeGenericMethod(dictionaryType.GetGenericArguments());
                setMeth.Invoke(null, new[] { collection, value });
                return;
            }
            var collectionType = type.GetInterface("ICollection`1");
            var addMeth = collectionType?.GetMethod("Add");
            if (addMeth == null) throw new ReplicateError($"Cannot deserialize repeated fields into {type.Name}");
            addMeth.Invoke(collection, new[] { value });
        }
    }
}
