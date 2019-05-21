using Replicate.MetaData.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public static class IRepNodeExtensions
    {
        public static bool IsSkipNull(this IRepNode node)
        {
            return node.MemberAccessor?.Info.GetAttribute<SkipNullAttribute>() != null;
        }
    }
}
