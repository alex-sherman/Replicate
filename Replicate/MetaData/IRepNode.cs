using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
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
        RepKey Key { get; set; }
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
    public struct RepKey
    {
        public int? Index;
        public string Name;
        public RepKey(string str) { Index = null; Name = str; }
        public RepKey(int index) { Index = index; Name = null; }

        public override bool Equals(object obj)
        {
            if (!(obj is RepKey)) return false;

            var key = (RepKey)obj;
            return EqualityComparer<int?>.Default.Equals(Index, key.Index) && Name == key.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = -1868479479;
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public static implicit operator RepKey(string str) => new RepKey(str);
        public static implicit operator RepKey(int index) => new RepKey(index);
        [ReplicateIgnore]
        public bool IsEmpty => Name == null && Index == null;
        public override string ToString()
        {
            if (Name != null) return Name;
            if (Index != null) return Index.ToString();
            return "<None>";
        }
    }
    [ReplicateType]
    public struct MethodKey
    {
        public TypeId Type;
        public RepKey Method;

        public override bool Equals(object obj)
        {
            if (!(obj is MethodKey))
            {
                return false;
            }

            var key = (MethodKey)obj;
            return EqualityComparer<TypeId>.Default.Equals(Type, key.Type) &&
                   EqualityComparer<RepKey>.Default.Equals(Method, key.Method);
        }

        public override int GetHashCode()
        {
            var hashCode = 314988997;
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeId>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<RepKey>.Default.GetHashCode(Method);
            return hashCode;
        }
        public override string ToString()
        {
            return $"{Type}.{Method}";
        }
    }
    public interface IRepObject : IRepNode, IEnumerable<KeyValuePair<RepKey, IRepNode>>
    {
        void EnsureConstructed();
        IRepNode this[RepKey key] { get; set; }
        bool CanSetMember(RepKey key);
    }

}
