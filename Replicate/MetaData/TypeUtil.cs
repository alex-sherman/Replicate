using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public static class TypeUtil
    {
        public static bool IsSameGeneric(this Type compare, Type target)
        {
            return (compare.IsGenericTypeDefinition && compare == target) || 
                (compare.IsGenericType && compare.GetGenericTypeDefinition() == target);
        }
    }
}
