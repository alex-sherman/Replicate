using Replicate.MetaTyping;
using System.Linq;

namespace Replicate.MetaData {
    public static class TupleSurrogate {
        public static void SetTupleSurrogate(this TypeData typeData) {
            var surrogateType = typeData.Model.Add(Fake.FromType(typeData.Type, typeData.Model));
            typeData.SetSurrogate(new Surrogate(
                surrogateType.Type,
                (orig, surrogate) => (_, obj) => {
                    if (obj == null) return null;
                    var result = surrogate.Construct();
                    return TypeUtil.CopyToRaw(obj, orig, result, surrogate);
                },
                (orig, surrogate) => (_, obj) => {
                    if (obj == null) return null;
                    return orig.Construct(surrogate.Members.Values.Select(member => member.GetValue(obj)).ToArray());
                }
            ));
        }
    }
}
