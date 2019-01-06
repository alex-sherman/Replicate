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
                return Model.GetRepNode(value, childTypeAccessor);
            }
            set => Backing[memberName] = (T)value.Value;
        }

        public IRepNode this[int memberIndex] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object Value { get => Backing; set => Backing = (Dictionary<string, T>)value; }

        public TypeAccessor TypeAccessor { get; }

        public MarshalMethod MarshalMethod => MarshalMethod.Object;
        public IRepPrimitive AsPrimitive => throw new NotImplementedException();
        public IRepCollection AsCollection => throw new NotImplementedException();
        public IRepObject AsObject => this;

        public IEnumerator<KeyValuePair<string, IRepNode>> GetEnumerator()
        {
            return Backing
                .Select(kvp => new KeyValuePair<string, IRepNode>(kvp.Key, Model.GetRepNode(kvp.Value, childTypeAccessor)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
