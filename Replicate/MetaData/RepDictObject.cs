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
        public RepDictObject(Dictionary<string, T> backing, TypeAccessor typeAccessor, ReplicationModel model)
        {
            Backing = backing;
            Model = model;
            childTypeAccessor = model.GetTypeAccessor(typeof(T));
            TypeAccessor = typeAccessor;
        }
        public IRepNode this[string memberName]
        {
            get
            {
                Backing.TryGetValue(memberName, out var value);
                var node = Model.GetRepNode(value, childTypeAccessor);
                node.Key = memberName;
                return node;
            }
            set => Backing[memberName] = (T)value.Value;
        }

        public IRepNode this[int memberIndex] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Key { get; set; }
        public object Value { get => Backing; set => Backing = (Dictionary<string, T>)value; }
        public TypeAccessor TypeAccessor { get; }

        public MarshalMethod MarshalMethod => MarshalMethod.Object;
        public IRepPrimitive AsPrimitive => throw new NotImplementedException();
        public IRepCollection AsCollection => throw new NotImplementedException();
        public IRepObject AsObject => this;

        public IEnumerator<IRepNode> GetEnumerator()
        {
            var @this = this;
            return Backing.Keys
                .Select(key => @this[key])
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed() => Backing = new Dictionary<string, T>();

        public bool CanSetMember(string memberName) => true;
    }
}
