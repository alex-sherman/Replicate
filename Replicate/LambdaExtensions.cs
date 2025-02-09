using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Replicate {
    public static class ReplicateExtensions {
        public static object[] EvaluateArguments(this MethodCallExpression method) {
            return method.Arguments.Select(arg => Expression.Lambda<Func<object>>(arg).Compile()()).ToArray();
        }
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value) {
            key = kvp.Key; value = kvp.Value;
        }
    }
}
