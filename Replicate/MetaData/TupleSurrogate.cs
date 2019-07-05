using Replicate.MetaTyping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public static class TupleSurrogate
    {
        public static void SetTupleSurrogate(this TypeData typeData)
        {
            var surrogateType = Fake.FromType(typeData.Type, typeData.Model);
            typeData.SetSurrogate(new Surrogate(
                surrogateType,
                (orig, surrogate) => obj =>
                {
                    if (obj == null) return null;
                    var result = surrogate.Construct();
                    return TypeUtil.CopyToRaw(obj, result);
                },
                (orig, surrogate) => obj =>
                {
                    if (obj == null) return null;
                    return orig.Construct(surrogate.MemberAccessors.Select(member => member.GetValue(obj)).ToArray());
                }
            ));
        }
    }
}
