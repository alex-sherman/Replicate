using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepNodeTypeless : IRepNode, IRepPrimitive, IRepCollection, IRepObject
    {
        public ReplicationModel Model { get; }
        public object RawValue => this;
        public RepKey Key { get; set; }
        public object Value { get; set; }

        public TypeAccessor TypeAccessor => Model.GetTypeAccessor(typeof(IRepNode));
        public MemberAccessor MemberAccessor { get; }

        public MarshallMethod MarshallMethod { get; set; } = MarshallMethod.None;
        public IRepPrimitive AsPrimitive { get { MarshallMethod = MarshallMethod.Primitive; return this; } }
        public IRepCollection AsCollection { get { MarshallMethod = MarshallMethod.Collection; return this; } }
        public IRepObject AsObject { get { MarshallMethod = MarshallMethod.Object; return this; } }
        public PrimitiveType PrimitiveType { get; set; }
        public TypeAccessor CollectionType => Model.GetTypeAccessor(typeof(IRepNode));

        public List<KeyValuePair<string, IRepNode>> Children = new List<KeyValuePair<string, IRepNode>>();
        public IEnumerable<object> Values
        {
            get => Children.Select(c => c.Value);
            set => Children = value.Select(v => new KeyValuePair<string, IRepNode>(null, (IRepNode)v)).ToList();
        }

        public RepNodeTypeless(ReplicationModel model, MemberAccessor memberAccessor = null)
        {
            Model = model;
            MemberAccessor = memberAccessor;
        }

        public IRepNode this[int memberIndex]
        {
            get => Children[memberIndex].Value;
            set => Children[memberIndex] = new KeyValuePair<string, IRepNode>(null, (IRepNode)value);
        }
        public IRepNode this[RepKey memberName]
        {
            get
            {
                if (memberName.Index.HasValue) return Children[memberName.Index.Value].Value;
                if (memberName.Index != null) return Children[memberName.Index.Value].Value;
                if(memberName.Name != null)
                {
                    var child = Children.Where(c => c.Key == memberName.Name).Select(c => c.Value).FirstOrDefault();
                    if (child == null)
                    {
                        child = new RepNodeTypeless(Model) { Key = memberName };
                        Children.Add(new KeyValuePair<string, IRepNode>(memberName.Name, child));
                    }
                    return child;
                }
                return null;
            }
            set => this[memberName].Value = value.Value;
        }

        public IEnumerator<KeyValuePair<RepKey, IRepNode>> GetEnumerator()
            => Children.Select(c => new KeyValuePair<RepKey, IRepNode>(c.Key, c.Value)).GetEnumerator();
        IEnumerator<IRepNode> IEnumerable<IRepNode>.GetEnumerator() => Children.Select(c => c.Value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() { }

        public override string ToString()
        {
            var result = MarshallMethod == MarshallMethod.Primitive ? (Value?.ToString() ?? "null") : $"{MarshallMethod.ToString()}";
            if (Key.Name != null)
                result = $"{Key}: {result}";
            return result;
        }

        public bool CanSetMember(RepKey _) => true;
    }
}
