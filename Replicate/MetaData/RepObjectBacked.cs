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
        public object RawValue
        {
            get
            {
                if (ConvertFromSurrogate != null)
                    return ConvertFromSurrogate(Value);
                return Value;
            }
            set
            {
                if (ConvertToSurrogate != null)
                    value = ConvertToSurrogate(value);
                Value = value;
            }
        }
        public ReplicationModel Model { get; private set; }
        public TypeAccessor TypeAccessor { get; set; }
        public MemberAccessor MemberAccessor { get; private set; }
        public Func<object, object> ConvertToSurrogate;
        public Func<object, object> ConvertFromSurrogate;
        public string Key { get; set; }
        public object Value { get; set; }

        public RepBackedNode(object backing, TypeAccessor typeAccessor = null,
            MemberAccessor memberAccessor = null, ReplicationModel model = null)
        {
            MemberAccessor = memberAccessor;
            Key = null;
            Model = model ?? ReplicationModel.Default;
            TypeAccessor = typeAccessor ?? Model.GetTypeAccessor(backing.GetType());
            if (TypeAccessor.Type.IsSameGeneric(typeof(Nullable<>)))
                TypeAccessor = Model.GetTypeAccessor(typeAccessor.Type.GetGenericArguments()[0]);

            var surrogate = MemberAccessor?.Surrogate ?? TypeAccessor.Surrogate;
            if (surrogate != null)
            {
                var castToOp = surrogate.Type.GetMethod("op_Implicit", new Type[] { typeAccessor.Type });
                ConvertToSurrogate = obj =>
                    obj == null ? null : castToOp.Invoke(null, new[] { obj });

                var castFromOp = surrogate.Type.GetMethod("op_Implicit", new Type[] { surrogate.Type });
                ConvertFromSurrogate = obj =>
                    obj == null ? null : castFromOp.Invoke(null, new[] { obj });
                TypeAccessor = surrogate;
                Value = ConvertToSurrogate(backing);
            }
            else
            {
                ConvertToSurrogate = null;
                ConvertFromSurrogate = null;
                Value = backing;
            }
            // TODO: Handle using a surrogate

        }
        public MarshalMethod MarshalMethod => TypeAccessor.TypeData.MarshalMethod;

        public PrimitiveType PrimitiveType
        {
            get => PrimitiveTypeMap.MapType(TypeAccessor.Type);
            set => throw new InvalidOperationException();
        }
        public IRepCollection AsCollection => new RepBackedCollection(this);

        public IRepObject AsObject => this;
        public IRepPrimitive AsPrimitive => this;

        #region Object Fields
        MemberAccessor[] MemberAccessors => TypeAccessor.MemberAccessors;
        public IRepNode this[int memberIndex]
        {
            get => this[MemberAccessors[memberIndex]];
            set => MemberAccessors[memberIndex].SetValue(Value, value.RawValue);
        }
        public IRepNode this[string memberName]
        {
            get => this[TypeAccessor.Members[memberName]];
            set => TypeAccessor.Members[memberName].SetValue(Value, value.RawValue);
        }
        IRepNode this[MemberAccessor member]
        {
            get
            {
                var node = Model.GetRepNode(member.GetValue(Value), member.TypeAccessor, member);
                node.Key = member.Info.Name;
                return node;
            }
        }

        public IEnumerator<IRepNode> GetEnumerator()
        {
            var @this = this;
            return MemberAccessors
                .Select(m => @this[m])
                .GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void EnsureConstructed()
        {
            Value = TypeAccessor.Construct();
        }
        public override string ToString()
        {
            return $"{Key}: {MarshalMethod.ToString()}";
        }

        public bool CanSetMember(string memberName) => TypeAccessor.Members.ContainsKey(memberName);
        #endregion
    }

    public struct RepBackedCollection : IRepCollection
    {
        private RepBackedNode Node;
        public object RawValue => Node.RawValue;
        public string Key { get; set; }
        public object Value { get => Node.Value; set => Node.Value = value; }
        public RepBackedCollection(RepBackedNode node)
        {
            Node = node;
            Key = null;
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
                yield return Node.Model.GetRepNode(item, CollectionType, null);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
