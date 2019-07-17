﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepNodeTypeless : IRepNode, IRepPrimitive, IRepCollection, IRepObject
    {
        public object RawValue => this;
        public MemberKey Key { get; set; }
        public object Value { get; set; }

        public TypeAccessor TypeAccessor => null;
        public MemberAccessor MemberAccessor { get; }

        private MarshalMethod? marshalMethod = null;
        public MarshalMethod MarshalMethod
        {
            get
            {
                if (!marshalMethod.HasValue)
                    throw new InvalidOperationException("MarshalMethod has not been set or inferred yet");
                return marshalMethod.Value;
            }
            set => marshalMethod = value;
        }
        public IRepPrimitive AsPrimitive { get { MarshalMethod = MarshalMethod.Primitive; return this; } }
        public IRepCollection AsCollection { get { MarshalMethod = MarshalMethod.Collection; return this; } }
        public IRepObject AsObject { get { MarshalMethod = MarshalMethod.Object; return this; } }
        public PrimitiveType PrimitiveType { get; set; }
        public TypeAccessor CollectionType => null;

        public List<RepNodeTypeless> Children = new List<RepNodeTypeless>();
        public IEnumerable<object> Values { get => Children; set => Children = value.Cast<RepNodeTypeless>().ToList(); }

        public RepNodeTypeless(MemberAccessor memberAccessor = null)
        {
            MemberAccessor = memberAccessor;
        }

        public IRepNode this[int memberIndex] { get => Children[memberIndex]; set => Children[memberIndex] = (RepNodeTypeless)value; }
        public IRepNode this[MemberKey memberName]
        {
            get
            {
                if (memberName.Index.HasValue) return Children[memberName.Index.Value];
                var child = Children.FirstOrDefault(c => c.Key.Equals(memberName));
                if (child == null)
                {
                    child = new RepNodeTypeless() { Key = memberName };
                    Children.Add(child);
                }
                return child;
            }
            set => this[memberName].Value = value.Value;
        }

        public IEnumerator<IRepNode> GetEnumerator() => Children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() { }

        public override string ToString()
        {
            var result = MarshalMethod == MarshalMethod.Primitive ? (Value?.ToString() ?? "null") : $"{MarshalMethod.ToString()}";
            if (Key.Name != null)
                result = $"{Key}: {result}";
            return result;
        }

        public bool CanSetMember(MemberKey _) => true;
    }
}
