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
    public interface IRepObject : IRepNode, IEnumerable<KeyValuePair<RepKey, IRepNode>>
    {
        void EnsureConstructed();
        IRepNode this[RepKey key] { get; set; }
        bool CanSetMember(RepKey key);
    }

}
