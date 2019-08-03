using Replicate.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class RepBackedNode : IRepNode, IRepObject, IRepPrimitive
    {
        public object RawValue
        {
            get
            {
                if (surrogate?.ConvertFrom != null)
                    return surrogate.ConvertFrom(Value);
                return Value;
            }
            set
            {
                if (surrogate?.ConvertTo != null)
                    value = surrogate.ConvertTo(value);
                Value = value;
            }
        }
        public ReplicationModel Model { get; private set; }
        public TypeAccessor TypeAccessor { get; set; }
        public MemberAccessor MemberAccessor { get; private set; }
        private readonly SurrogateAccessor surrogate;
        public RepKey Key { get; set; }
        private object _value;
        public object Value
        {
            get => _value;
            set => _value = MarshallMethod == MarshallMethod.Primitive ? TypeAccessor.Coerce(value) : value;
        }

        public RepBackedNode(object backing, TypeAccessor typeAccessor = null,
            MemberAccessor memberAccessor = null, ReplicationModel model = null)
        {
            MemberAccessor = memberAccessor;
            Model = model ?? ReplicationModel.Default;
            var originalTypeAccessor = typeAccessor ?? Model.GetTypeAccessor(backing.GetType());
            surrogate = memberAccessor?.Surrogate ?? originalTypeAccessor.Surrogate;
            TypeAccessor = surrogate?.TypeAccessor ?? originalTypeAccessor;
            RawValue = backing;
            if (MarshallMethod == MarshallMethod.Primitive)
                _primitiveType = PrimitiveTypeMap.MapType(TypeAccessor.Type);
        }
        public MarshallMethod MarshallMethod => TypeAccessor.TypeData.MarshallMethod;

        private PrimitiveType _primitiveType;
        public PrimitiveType PrimitiveType
        {
            get => _primitiveType;
            set => throw new InvalidOperationException();
        }
        public IRepCollection AsCollection => new RepBackedCollection(this);

        public IRepObject AsObject => this;
        public IRepPrimitive AsPrimitive => this;

        #region Object Fields
        MemberAccessor[] MemberAccessors => TypeAccessor.MemberAccessors;
        public IRepNode this[RepKey key]
        {
            get => this[TypeAccessor[key]];
            set
            {
                var member = TypeAccessor[key];
                if (member == null) throw new KeyNotFoundException(key.ToString());
                member.SetValue(Value, value.RawValue);
            }
        }
        IRepNode this[MemberAccessor member]
        {
            get
            {
                return Model.GetRepNode(member.GetValue(Value), member.TypeAccessor, member);
            }
        }

        public IEnumerator<KeyValuePair<RepKey, IRepNode>> GetEnumerator()
        {
            var @this = this;
            return TypeAccessor.TypeData.Keys
                .Select(m => new KeyValuePair<RepKey, IRepNode>(m, @this[m]))
                .GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed()
        {
            Value = TypeAccessor.Construct();
        }
        public override string ToString()
        {
            return $"{Key}: {MarshallMethod.ToString()}";
        }

        public bool CanSetMember(RepKey key) => TypeAccessor[key] != null;
        #endregion
    }

    public struct RepBackedCollection : IRepCollection
    {
        private RepBackedNode Node;
        public object RawValue => Node.RawValue;
        public RepKey Key { get; set; }
        public object Value { get => Node.Value; set => Node.Value = value; }
        public RepBackedCollection(RepBackedNode node)
        {
            Node = node;
            Key = node.Key;
            CollectionType = node.Model.GetCollectionValueAccessor(node.TypeAccessor.Type);
        }
        public TypeAccessor CollectionType { get; private set; }
        public TypeAccessor TypeAccessor { get => Node.TypeAccessor; }
        public MemberAccessor MemberAccessor { get => Node.MemberAccessor; }

        public IEnumerable<object> Values
        {
            get => _values();
            set
            {
                Node.Value = CollectionUtil.FillCollection(Node.Value, TypeAccessor.Type, value?.ToList());
            }
        }

        public MarshallMethod MarshallMethod => MarshallMethod.Collection;
        public IRepPrimitive AsPrimitive => throw new InvalidOperationException();
        public IRepObject AsObject => throw new InvalidOperationException();
        public IRepCollection AsCollection => this;

        IEnumerable<object> _values()
        {
            foreach (var item in (IEnumerable)Node.Value)
                yield return item;
        }

        public IEnumerator<IRepNode> GetEnumerator()
        {
            var collectionType = CollectionType;
            foreach (var item in Values)
                yield return Node.Model.GetRepNode(item, CollectionType, null);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
