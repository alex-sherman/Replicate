using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepDictObject<T> : IRepObject
    {
        Dictionary<string, T> Backing;
        object IRepNode.RawValue => Backing;
        ReplicationModel Model;
        TypeAccessor childTypeAccessor;
        public RepDictObject(Dictionary<string, T> backing, TypeAccessor typeAccessor,
            MemberAccessor memberAccessor, ReplicationModel model)
        {
            Backing = backing;
            Model = model;
            childTypeAccessor = model.GetTypeAccessor(typeof(T));
            TypeAccessor = typeAccessor;
            MemberAccessor = memberAccessor;
        }
        public IRepNode this[MemberKey key]
        {
            get
            {
                Backing.TryGetValue(key.Name, out var value);
                var node = Model.GetRepNode(value, childTypeAccessor, null);
                return node;
            }
            set => Backing[key.Name] = (T)value.Value;
        }

        public MemberKey Key { get; set; }
        public object Value { get => Backing; set => Backing = (Dictionary<string, T>)value; }
        public TypeAccessor TypeAccessor { get; }
        public MemberAccessor MemberAccessor { get; }

        public MarshallMethod MarshallMethod => MarshallMethod.Object;
        public IRepPrimitive AsPrimitive => null;
        public IRepCollection AsCollection => null;
        public IRepObject AsObject => this;

        public IEnumerator<KeyValuePair<MemberKey, IRepNode>> GetEnumerator()
        {
            var @this = this;
            return Backing.Keys
                .Select(key => new KeyValuePair<MemberKey, IRepNode>(key, @this[key]))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() => Backing = new Dictionary<string, T>();

        public bool CanSetMember(MemberKey _) => true;
    }
}
