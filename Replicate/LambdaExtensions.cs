using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public static class LambdaExtensions
    {
        public static object[] EvaluateArguments(this MethodCallExpression method)
        {
            return method.Arguments.Select(arg => Expression.Lambda<Func<object>>(arg).Compile()()).ToArray();
        }
    }
}
