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
        public RepDictObject(Dictionary<string, T> backing, ReplicationModel model)
        {
            Backing = backing;
            Model = model;
            childTypeAccessor = model.GetTypeAccessor(typeof(T));
        }
        public IRepNode this[string memberName] => Model.GetRepNode(Backing[memberName], childTypeAccessor);

        public IRepNode this[int memberIndex] => throw new NotImplementedException();

        public object Value { get => Backing; set => throw new NotImplementedException(); }

        public TypeAccessor TypeAccessor => throw new NotImplementedException();

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
