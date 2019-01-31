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
        /// <summary>
        /// The true, non-surrogatted, value represented by this node
        /// </summary>
        object RawValue { get; }
        /// <summary>
        /// The, potentially surrogatted, value represented by this node
        /// </summary>
        object Value { get; set; }
        string Key { get; set; }
        TypeAccessor TypeAccessor { get; }
        MarshalMethod MarshalMethod { get; }
        IRepPrimitive AsPrimitive { get; }
        IRepCollection AsCollection { get; }
        IRepObject AsObject { get; }
    }
    public interface IRepPrimitive : IRepNode
    {
        PrimitiveType PrimitiveType { get; set; }
    }
    public interface IRepCollection : IRepNode, IEnumerable<IRepNode>
    {
        TypeAccessor CollectionType { get; }
        IEnumerable<object> Values { get; set; }
    }
    public interface IRepObject : IRepNode, IEnumerable<IRepNode>
    {
        void EnsureConstructed();
        IRepNode this[string memberName] { get; set; }
        IRepNode this[int memberIndex] { get; set; }
    }

}
