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
        public string Key { get; set; }
        public object Value { get; set; }

        public TypeAccessor TypeAccessor => null;
        public MemberAccessor MemberAccessor => null;

        public MarshalMethod MarshalMethod { get => throw new NotImplementedException(); }
        public IRepPrimitive AsPrimitive => this;
        public IRepCollection AsCollection => this;
        public IRepObject AsObject => this;

        public PrimitiveType PrimitiveType { get; set; }

        public TypeAccessor CollectionType => null;

        static readonly IEnumerable<RepNodeNoop> Children = Enumerable.Empty<RepNodeNoop>();
        public IEnumerable<object> Values { get => Children; set { } }

        public IRepNode this[int memberIndex] { get => throw new KeyNotFoundException(); set { } }
        public IRepNode this[string memberName]
        {
            get => this;
            set { }
        }

        public IEnumerator<IRepNode> GetEnumerator() => Children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() { }

        public bool CanSetMember(string memberName) => false;
    }
}
