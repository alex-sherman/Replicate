using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class Surrogate
    {
        public delegate object Conversion(object source);
        public delegate Conversion ConversionGenerator(TypeAccessor originalType, TypeAccessor surrogateType);
        public readonly Type Type;
        public readonly ConversionGenerator GetConvertTo;
        public readonly ConversionGenerator GetConvertFrom;
        public readonly Func<Type, Type> GetSurrogateType;
        public Surrogate(Type type, ConversionGenerator getConvertTo = null,
            ConversionGenerator getConvertFrom = null, Func<Type, Type> getSurrogateType = null)
        {
            Type = type;
            GetSurrogateType = getSurrogateType ?? (originalType =>
            {
                // TODO: This is untested and maybe confusing?
                if (!Type.IsGenericTypeDefinition) return Type;
                return Type.MakeGenericType(originalType.GetGenericArguments());
            });
            GetConvertTo = getConvertTo ?? ((originalAccessor, surrogateAccessor) =>
            {
                var originalType = originalAccessor.Type;
                var surrogateType = surrogateAccessor.Type;
                var castToOp = surrogateType.GetMethod("op_Implicit", new Type[] { originalType });
                if (castToOp != null)
                    return obj => obj == null ? null : castToOp.Invoke(null, new[] { obj });
                // TODO: Defaulting to copying members is confusing
                // should maybe just be explicit for fakes
                else
                    return obj => TypeUtil.CopyToRaw(obj, originalType, Activator.CreateInstance(surrogateType), surrogateType);
            });
            GetConvertFrom = getConvertFrom ?? ((originalAccessor, surrogateAccessor) =>
            {
                var originalType = originalAccessor.Type;
                var surrogateType = surrogateAccessor.Type;
                var castFromOp = surrogateType.GetMethod("op_Implicit", new Type[] { surrogateType });
                if (castFromOp != null)
                    return obj => obj == null ? null : castFromOp.Invoke(null, new[] { obj });
                else
                    return obj => TypeUtil.CopyToRaw(obj, surrogateType, Activator.CreateInstance(originalType), originalType);
            });
        }
        public static implicit operator Surrogate(Type type) => new Surrogate(type);
    }
    public class SurrogateAccessor
    {
        public readonly TypeAccessor TypeAccessor;
        public readonly Surrogate.Conversion ConvertTo;
        public readonly Surrogate.Conversion ConvertFrom;
        private SurrogateAccessor() { }
        public SurrogateAccessor(TypeAccessor originalTypeAccessor, Surrogate surrogate, ReplicationModel model)
        {
            var originalType = originalTypeAccessor.Type;
            var surrogateType = surrogate.GetSurrogateType(originalType);
            TypeAccessor = model.GetTypeAccessor(surrogateType);
            ConvertTo = surrogate.GetConvertTo(originalTypeAccessor, TypeAccessor);
            ConvertFrom = surrogate.GetConvertFrom(originalTypeAccessor, TypeAccessor);
        }
    }
}
