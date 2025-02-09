using Replicate.MetaData;
using System;
using System.IO;

namespace Replicate.Serialization {
    public abstract class GraphSerializer : IReplicateSerializer {
        public ReplicationModel Model { get; private set; }
        public GraphSerializer(ReplicationModel model) {
            Model = model;
        }
        private GraphSerializer() { }

        public Stream Serialize(Type type, object obj, Stream stream) {
            Write(stream, Model.GetRepNode(obj, type));
            return stream;
        }

        public T Deserialize<T>(Stream wireValue) => (T)Deserialize(typeof(T), wireValue);
        public object Deserialize(Type type, Stream wireValue, object existing = null) {
            var node = Model.GetRepNode(existing, type);
            return Read(wireValue, node).RawValue;
        }
        public void Write(Stream context, IRepNode node) {
            switch (node.MarshallMethod) {
                case MarshallMethod.Primitive:
                    Write(context, node.AsPrimitive);
                    break;
                case MarshallMethod.Collection:
                    Write(context, node.AsCollection);
                    break;
                //case MarshalMethod.Tuple:
                //    SerializeTuple(stream, obj, typeAccessor);
                //    break;
                case MarshallMethod.Object:
                    Write(context, node.AsObject);
                    break;
            }
        }
        public abstract void Write(Stream context, IRepPrimitive value);
        public abstract void Write(Stream context, IRepCollection value);
        public abstract void Write(Stream context, IRepObject value);
        public abstract (MarshallMethod, PrimitiveType?) ReadNodeType(Stream context);
        public IRepNode Read(Stream context, IRepNode node) {
            var marshalMethod = node.MarshallMethod;
            (MarshallMethod, PrimitiveType?) nodeInfo = (node.MarshallMethod, null);
            if (marshalMethod == MarshallMethod.None) {
                nodeInfo = ReadNodeType(context);
                marshalMethod = nodeInfo.Item1;
            }
            switch (marshalMethod) {
                case MarshallMethod.Primitive:
                    var primitive = node.AsPrimitive;
                    if (nodeInfo.Item2.HasValue) primitive.PrimitiveType = nodeInfo.Item2.Value;
                    return Read(context, primitive);
                case MarshallMethod.Collection:
                    return Read(context, node.AsCollection);
                //case MarshalMethod.Tuple:
                //    return node.Value = Read(context, node.AsPrimitive);
                case MarshallMethod.Object:
                    return Read(context, node.AsObject);
                default:
                    node.Value = Default(node.TypeAccessor.Type);
                    return node;
            }
        }
        protected static object Default(Type type) {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
        public abstract IRepPrimitive Read(Stream context, IRepPrimitive value);
        public abstract IRepCollection Read(Stream context, IRepCollection value);
        public abstract IRepObject Read(Stream context, IRepObject value);
    }
}
