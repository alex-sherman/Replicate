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
        static Dictionary<Type, PrimitiveType> Map = new Dictionary<Type, PrimitiveType>()
        {
            { typeof(bool),   PrimitiveType.Bool },
            { typeof(byte),   PrimitiveType.Int8 },
            { typeof(int),    PrimitiveType.Int32 },
            { typeof(ushort),   PrimitiveType.Int32 },
            { typeof(uint),   PrimitiveType.Int32 },
            { typeof(float),  PrimitiveType.Float },
            { typeof(double), PrimitiveType.Double },
            { typeof(string), PrimitiveType.String },
        };
        public static PrimitiveType MapType(Type type)
        {
            if (type.IsEnum)
                return PrimitiveType.Int32;
            return Map[type];
        }
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
        MemberKey Key { get; set; }
        TypeAccessor TypeAccessor { get; }
        MemberAccessor MemberAccessor { get; }
        MarshallMethod MarshallMethod { get; }
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
    [ReplicateType]
    public struct MemberKey
    {
        public int? Index;
        public string Name;
        public MemberKey(string str) { Index = null; Name = str; }
        public MemberKey(int index) { Index = index; Name = null; }

        public override bool Equals(object obj)
        {
            if (!(obj is MemberKey)) return false;

            var key = (MemberKey)obj;
            return EqualityComparer<int?>.Default.Equals(Index, key.Index) && Name == key.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = -1868479479;
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public static implicit operator MemberKey(string str) => new MemberKey(str);
        public static implicit operator MemberKey(int index) => new MemberKey(index);
        public bool IsEmpty => Name != null || Index != null;
        public override string ToString()
        {
            if (Name != null) return Name;
            if (Index != null) return Index.ToString();
            return "<None>";
        }
    }
    public interface IRepObject : IRepNode, IEnumerable<KeyValuePair<MemberKey, IRepNode>>
    {
        void EnsureConstructed();
        IRepNode this[MemberKey key] { get; set; }
        bool CanSetMember(MemberKey key);
    }

}
