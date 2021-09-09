using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public static class CollectionUtil
    {
        public static object FillCollection(object collection, Type type, List<object> values)
        {
            var count = values.Count;

            // Interfaces cannot be constructed, so use a List to implement these interfaces
            if (type.IsSameGeneric(typeof(IEnumerable<>)) || type.IsSameGeneric(typeof(ICollection<>)))
            {
                type = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                collection = null;
            }
            var collectionType = type.GetInterface("ICollection`1");
            if (collection is Array || collection == null)
                collection = Activator.CreateInstance(type, count);
            else
            {
                var clearMeth = collectionType.GetMethod("Clear");
                clearMeth.Invoke(collection, new object[] { });
            }
            if (collection is Array)
            {
                var arr = collection as Array;
                for (int i = 0; i < count; i++)
                    arr.SetValue(values[i], i);
            }
            else
            {
                var addMeth = collectionType.GetMethod("Add");
                foreach (var value in values)
                    addMeth.Invoke(collection, new object[] { value });
            }
            return collection;
        }
    }
}
