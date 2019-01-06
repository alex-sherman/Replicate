using Replicate.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public struct RepBackedNode : IRepNode, IRepObject, IRepPrimitive
    {
        public object Backing { get; private set; }
        public ReplicationModel Model { get; private set; }
        public TypeAccessor TypeAccessor { get; set; }
        public MemberAccessor MemberAccessor;
        public Func<object, object> ConvertToSurrogate;
        public Func<object, object> ConvertFromSurrogate;
        public object Value
        {
            get
            {
                var output = Backing;
                if (ConvertToSurrogate != null)
                    output = ConvertToSurrogate(output);
                return output;
            }
            set
            {
                if (ConvertFromSurrogate != null)
                    value = ConvertFromSurrogate(value);
                Backing = value;
            }
        }

        public RepBackedNode(object backing, TypeAccessor typeAccessor = null,
            MemberAccessor memberAccessor = null, ReplicationModel model = null)
        {
            MemberAccessor = memberAccessor;
            Backing = backing;
            Model = model ?? ReplicationModel.Default;
            TypeAccessor = typeAccessor ?? Model.GetTypeAccessor(backing.GetType());
            if (TypeAccessor.Type.IsSameGeneric(typeof(Nullable<>)))
                TypeAccessor = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[0]);
            ConvertFromSurrogate = null;
            ConvertToSurrogate = null;
            // TODO: Handle using a surrogate

        }
        public MarshalMethod MarshalMethod => TypeAccessor.TypeData.MarshalMethod;

        public PrimitiveType PrimitiveType => PrimitiveTypeMap.Map[TypeAccessor.Type];
        public IRepCollection AsCollection => new RepBackedCollection(this);

        public IRepObject AsObject => this;
        public IRepPrimitive AsPrimitive => this;

        #region Object Fields
        MemberAccessor[] MemberAccessors => TypeAccessor.MemberAccessors;
        public IRepNode this[int memberIndex]
        {
            get => this[MemberAccessors[memberIndex]];
            set => MemberAccessors[memberIndex].SetValue(Backing, value.Backing);
        }
        public IRepNode this[string memberName]
        {
            get => this[MemberAccessors.First(m => m.Info.Name == memberName)];
            set => MemberAccessors.First(m => m.Info.Name == memberName).SetValue(Backing, value.Backing);
        }
        IRepNode this[MemberAccessor member] => Model.GetRepNode(member.GetValue(Value), member.TypeAccessor, member);

        public IEnumerator<KeyValuePair<string, IRepNode>> GetEnumerator()
        {
            var @this = this;
            return MemberAccessors
                .Select(m => new KeyValuePair<string, IRepNode>(m.Info.Name, @this[m]))
                .GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }

    public struct RepBackedCollection : IRepCollection
    {
        private RepBackedNode Node;
        object IRepNode.Backing => Node.Backing;
        object IRepNode.Value { get => Node.Value; set => Node.Value = value; }
        public RepBackedCollection(RepBackedNode node)
        {
            Node = node;
            CollectionType = node.Model.GetCollectionValueAccessor(node.TypeAccessor.Type);
        }
        public TypeAccessor CollectionType { get; private set; }
        public TypeAccessor TypeAccessor { get => Node.TypeAccessor; }

        public IEnumerable<object> Values
        {
            get => _values();
            set
            {
                Node.Value = Serializer.FillCollection(Node.Value, TypeAccessor.Type, value?.ToList());
            }
        }

        public MarshalMethod MarshalMethod => MarshalMethod.Collection;
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
                yield return Node.Model.GetRepNode(item, CollectionType);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
