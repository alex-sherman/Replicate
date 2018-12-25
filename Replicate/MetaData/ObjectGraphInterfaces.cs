using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public enum PrimitiveType
    {
        Bool = 1,
        Int8 = 2,
        Int32 = 4,
        Float = 6,
        Double = 7,
        String = 8,
    }
    public static class PrimitiveTypeMap
    {
        public static Dictionary<Type, PrimitiveType> Map = new Dictionary<Type, PrimitiveType>()
        {
            { typeof(bool),   PrimitiveType.Bool },
            { typeof(byte),   PrimitiveType.Int8 },
            { typeof(int),    PrimitiveType.Int32 },
            { typeof(uint),   PrimitiveType.Int32 },
            { typeof(float),  PrimitiveType.Float },
            { typeof(double), PrimitiveType.Double },
            { typeof(string), PrimitiveType.String },
        };
    }
    public interface IRepNode
    {
        MarshalMethod MarshalMethod { get; }
        IRepPrimitive AsPrimitive { get; }
        IRepCollection AsCollection { get; }
        IRepObject AsObject { get; }
    }
    public interface IRepPrimitive : IRepNode
    {
        object Value { get; set; }
        PrimitiveType PrimitiveType { get; }
    }
    public interface IRepCollection : IRepNode, IEnumerable<IRepNode>
    {
        IEnumerable<object> Value { get; set; }
    }
    public interface IRepObject : IRepNode, IEnumerable<KeyValuePair<MemberAccessor, IRepNode>>
    {
        object Value { get; set; }
        IRepNode this[MemberAccessor member] { get; }
        IRepNode this[string memberName] { get; }
        IRepNode this[int memberIndex] { get; }
    }

}
