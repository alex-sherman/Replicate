using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Replicate.MetaData {
    public class Surrogate {
        public delegate object Conversion(IReplicateSerializer serializer, object source);
        public delegate Conversion ConversionGenerator(TypeAccessor originalType, TypeAccessor surrogateType);
        public readonly ConversionGenerator GetConvertTo;
        public readonly ConversionGenerator GetConvertFrom;
        public readonly Func<Type, Type> GetSurrogateType;
        public static Surrogate Simple<O, S>(Func<O, S> convertTo, Func<S, O> convertFrom) {
            return new Surrogate(typeof(S),
                (o, s) => (r, t) => t == null ? null : (object)convertTo((O)t),
                (o, s) => (r, u) => u == null ? null : (object)convertFrom((S)u));
        }
        public static Surrogate EnumAsString<E>() where E : struct {
            return Simple<E, string>(e => e.ToString(), s => Enum.TryParse<E>(s, out var e) ? e : default);
        }
        public static T[] ListToArray<T>(List<T> list) => list?.ToArray();
        public static List<T> ArrayToList<T>(T[] array) => array?.ToList();
        public static Surrogate ArrayAsList = new Surrogate(
            t => typeof(List<>).MakeGenericType(t.GetElementType()),
            (ot, st) => {
                var meth = typeof(Surrogate).GetMethod(nameof(ArrayToList)).MakeGenericMethod(ot.Type.GetElementType());
                return (r, o) => meth.Invoke(null, new[] { o });
            }, (ot, st) => {
                var meth = typeof(Surrogate).GetMethod(nameof(ListToArray)).MakeGenericMethod(ot.Type.GetElementType());
                return (r, o) => meth.Invoke(null, new[] { o });
            });
        public Surrogate(Type type, ConversionGenerator getConvertTo = null,
            ConversionGenerator getConvertFrom = null) : this(originalType => {
                // TODO: This is untested and maybe confusing?
                if (!type.IsGenericTypeDefinition) return type;
                return type.MakeGenericType(originalType.GetGenericArguments());
            }, getConvertTo, getConvertFrom) { }
        public Surrogate(Func<Type, Type> getSurrogateType, ConversionGenerator getConvertTo = null,
            ConversionGenerator getConvertFrom = null) {
            GetSurrogateType = getSurrogateType;
            GetConvertTo = getConvertTo ?? ((originalAccessor, surrogateAccessor) => {
                var originalType = originalAccessor.Type;
                var surrogateType = surrogateAccessor.Type;
                var castToOp = surrogateType.GetMethod("op_Implicit", new Type[] { originalType });
                if (castToOp != null)
                    return (_, obj) => obj == null ? null : castToOp.Invoke(null, new[] { obj });
                // TODO: Defaulting to copying members is confusing
                // should maybe just be explicit for fakes
                return (_, obj) => TypeUtil.CopyToRaw(obj, originalType, Activator.CreateInstance(surrogateType), surrogateType);
            });
            GetConvertFrom = getConvertFrom ?? ((originalAccessor, surrogateAccessor) => {
                var originalType = originalAccessor.Type;
                var surrogateType = surrogateAccessor.Type;
                var castFromOp = surrogateType.GetMethod("op_Implicit", new Type[] { surrogateType });
                if (castFromOp != null)
                    return (_, obj) => obj == null ? null : castFromOp.Invoke(null, new[] { obj });
                return (_, obj) => TypeUtil.CopyToRaw(obj, surrogateType, Activator.CreateInstance(originalType), originalType);
            });
        }
        public static implicit operator Surrogate(Type type) => new Surrogate(type);
    }
    public class SurrogateAccessor {
        public readonly TypeAccessor TypeAccessor;
        public readonly Surrogate.Conversion ConvertTo;
        public readonly Surrogate.Conversion ConvertFrom;
        private SurrogateAccessor() { }
        public SurrogateAccessor(TypeAccessor originalTypeAccessor, Surrogate surrogate, ReplicationModel model) {
            var originalType = originalTypeAccessor.Type;
            var surrogateType = surrogate.GetSurrogateType(originalType);
            TypeAccessor = model.GetTypeAccessor(surrogateType);
            ConvertTo = surrogate.GetConvertTo(originalTypeAccessor, TypeAccessor);
            ConvertFrom = surrogate.GetConvertFrom(originalTypeAccessor, TypeAccessor);
        }
    }
}
