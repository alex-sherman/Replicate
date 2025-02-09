using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Replicate.MetaData {
    public class RepNodeNoop : IRepNode, IRepPrimitive, IRepCollection, IRepObject {
        public static RepNodeNoop Single { get; } = new RepNodeNoop();
        public object RawValue => this;
        public RepKey Key { get; set; }
        public object Value { get; set; }

        public TypeAccessor TypeAccessor => null;
        public MemberAccessor MemberAccessor => null;

        public MarshallMethod MarshallMethod { get => MarshallMethod.None; }
        public IRepPrimitive AsPrimitive => this;
        public IRepCollection AsCollection => this;
        public IRepObject AsObject => this;

        public PrimitiveType PrimitiveType { get; set; }

        public TypeAccessor CollectionType => null;

        static readonly IEnumerable<KeyValuePair<RepKey, RepNodeNoop>> Children = Enumerable.Empty<KeyValuePair<RepKey, RepNodeNoop>>();
        public IEnumerable<object> Values { get => Enumerable.Empty<object>(); set { } }

        public IRepNode this[RepKey memberName] {
            get => this;
            set { }
        }

        public IEnumerator<KeyValuePair<RepKey, IRepNode>> GetEnumerator() => Enumerable.Empty<KeyValuePair<RepKey, IRepNode>>().GetEnumerator();
        IEnumerator<IRepNode> IEnumerable<IRepNode>.GetEnumerator() => Enumerable.Empty<IRepNode>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() { }

        public bool CanSetMember(RepKey _) => false;

    }
}
