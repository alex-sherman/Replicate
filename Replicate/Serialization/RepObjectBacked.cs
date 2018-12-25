using Replicate.MetaData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public struct RepBackedNode : IRepNode, IRepObject, IRepPrimitive
    {
        object Backing;
        public ReplicationModel Model { get; private set; }
        public TypeAccessor TypeAccessor;
        public TypeAccessor SurrogateAccessor;
        public MemberAccessor MemberAccessor;
        public Func<object, object> ConvertToSurrogate;
        public Func<object, object> ConvertFromSurrogate;
        // TODO: Account for surrogate
        // TODO: Throw errors if trying to set value on a primitive that is not a child maybe? (The member == null case)
        public object Value
        {
            get
            {
                var output = Backing;
                if (MemberAccessor != null)
                    output = MemberAccessor.GetValue(Backing);
                if (ConvertToSurrogate != null)
                    output = ConvertToSurrogate(output);
                return output;
            }
            set
            {
                if (ConvertFromSurrogate != null)
                    value = ConvertFromSurrogate(value);
                if (MemberAccessor != null)
                    MemberAccessor.SetValue(Backing, value);
                else
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
            SurrogateAccessor = memberAccessor?.Surrogate ?? TypeAccessor.Surrogate;
            ConvertFromSurrogate = null;
            ConvertToSurrogate = null;
            if (SurrogateAccessor != null)
            {
                var castToOp = SurrogateAccessor.Type.GetMethod("op_Implicit", new Type[] { TypeAccessor.Type });
                ConvertToSurrogate = obj => castToOp.Invoke(null, new[] { obj });

                var castFromOp = SurrogateAccessor.Type.GetMethod("op_Implicit", new Type[] { SurrogateAccessor.Type });
                ConvertFromSurrogate = obj => castFromOp.Invoke(null, new[] { obj });
            }
            // TODO: Handle using a surrogate

        }
        public MarshalMethod MarshalMethod => TypeAccessor.TypeData.MarshalMethod;

        public PrimitiveType PrimitiveType => PrimitiveTypeMap.Map[TypeAccessor.Type];
        public IRepCollection AsCollection => new RepBackedCollection(this);

        public IRepObject AsObject => this;
        public IRepPrimitive AsPrimitive => this;

        #region Object Fields
        MemberAccessor[] MemberAccessors => (SurrogateAccessor ?? TypeAccessor).MemberAccessors;
        public IRepNode this[int memberIndex] => this[MemberAccessors[memberIndex]];
        public IRepNode this[string memberName] => this[MemberAccessors.First(m => m.Info.Name == memberName)];
        public IRepNode this[MemberAccessor member] => new RepBackedNode(Value, member.TypeAccessor, member, Model);

        public IEnumerator<KeyValuePair<MemberAccessor, IRepNode>> GetEnumerator()
        {
            var @this = this;
            return MemberAccessors
                .Select(m => new KeyValuePair<MemberAccessor, IRepNode>(m, @this[m]))
                .GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }

    public struct RepBackedCollection : IRepCollection
    {
        private RepBackedNode Node;
        public RepBackedCollection(RepBackedNode node)
        {
            Node = node;
            CollectionType = node.Model.GetCollectionValueAccessor(node.TypeAccessor.Type);
        }
        public TypeAccessor CollectionType { get; private set; }
        public TypeAccessor TypeAccessor { get => Node.TypeAccessor; }

        public IEnumerable<object> Value
        {
            get => Values();
            set
            {
                Node.Value = Serializer.FillCollection(Node.Value, TypeAccessor.Type, value.ToList());
            }
        }

        public MarshalMethod MarshalMethod => MarshalMethod.Collection;
        public IRepPrimitive AsPrimitive => throw new InvalidOperationException();
        public IRepObject AsObject => throw new InvalidOperationException();
        public IRepCollection AsCollection => this;

        IEnumerable<object> Values()
        {
            foreach (var item in (IEnumerable)Node.Value)
                yield return item;
        }

        public IEnumerator<IRepNode> GetEnumerator()
        {
            var collectionType = CollectionType;
            foreach (var item in Value)
                yield return new RepBackedNode(item, collectionType, model: Node.Model);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
