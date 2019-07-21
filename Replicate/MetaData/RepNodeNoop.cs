using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepNodeNoop : IRepNode, IRepPrimitive, IRepCollection, IRepObject
    {
        public static RepNodeNoop Single { get; } = new RepNodeNoop();
        public object RawValue => this;
        public MemberKey Key { get; set; }
        public object Value { get; set; }

        public TypeAccessor TypeAccessor => null;
        public MemberAccessor MemberAccessor => null;

        public MarshallMethod MarshallMethod { get => MarshallMethod.None; }
        public IRepPrimitive AsPrimitive => this;
        public IRepCollection AsCollection => this;
        public IRepObject AsObject => this;

        public PrimitiveType PrimitiveType { get; set; }

        public TypeAccessor CollectionType => null;

        static readonly IEnumerable<KeyValuePair<MemberKey, RepNodeNoop>> Children = Enumerable.Empty<KeyValuePair<MemberKey, RepNodeNoop>>();
        public IEnumerable<object> Values { get => Enumerable.Empty<object>(); set { } }

        public IRepNode this[MemberKey memberName]
        {
            get => this;
            set { }
        }

        public IEnumerator<KeyValuePair<MemberKey, IRepNode>> GetEnumerator() => Enumerable.Empty<KeyValuePair<MemberKey, IRepNode>>().GetEnumerator();
        IEnumerator<IRepNode> IEnumerable<IRepNode>.GetEnumerator() => Enumerable.Empty<IRepNode>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() { }

        public bool CanSetMember(MemberKey _) => false;

    }
}
