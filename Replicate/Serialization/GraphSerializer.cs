using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public abstract class GraphSerializer<TContext, TWireType> : IReplicateSerializer<TWireType>
    {
        public ReplicationModel Model { get; private set; }
        public GraphSerializer(ReplicationModel model)
        {
            Model = model;
        }
        private GraphSerializer() { }

        public TWireType Serialize<T>(T obj) => Serialize(typeof(T), obj);
        public TWireType Serialize(Type type, object obj)
        {
            var context = GetContext(default(TWireType));
            Write(context, Model.GetRepNode(obj, type));
            return GetWireValue(context);
        }

        public T Deserialize<T>(TWireType wireValue) => (T)Deserialize(typeof(T), wireValue);
        public object Deserialize(Type type, TWireType wireValue) => Deserialize(null, type, wireValue);
        public object Deserialize(object existingValue, Type type, TWireType wireValue)
        {
            var context = GetContext(wireValue);
            var node = Model.GetRepNode(existingValue, type);
            return Read(context, node);
        }
        public abstract TContext GetContext(TWireType wireValue);
        public abstract TWireType GetWireValue(TContext context);
        public void Write(TContext context, IRepNode node)
        {
            switch (node.MarshalMethod)
            {
                case MarshalMethod.Primitive:
                    Write(context, node.AsPrimitive);
                    break;
                case MarshalMethod.Collection:
                    Write(context, node.AsCollection);
                    break;
                //case MarshalMethod.Tuple:
                //    SerializeTuple(stream, obj, typeAccessor);
                //    break;
                case MarshalMethod.Object:
                    Write(context, node.AsObject);
                    break;
            }
        }
        public abstract void Write(TContext context, IRepPrimitive value);
        public abstract void Write(TContext context, IRepCollection value);
        public abstract void Write(TContext context, IRepObject value);
        public object Read(TContext context, IRepNode node)
        {
            switch (node.MarshalMethod)
            {
                case MarshalMethod.Primitive:
                    return node.Value = Read(context, node.AsPrimitive);
                case MarshalMethod.Collection:
                    return node.Value = Read(context, node.AsCollection);
                //case MarshalMethod.Tuple:
                //    return node.Value = Read(context, node.AsPrimitive);
                case MarshalMethod.Object:
                    return node.Value = Read(context, node.AsObject);
                default:
                    return Default(node.TypeAccessor.Type);
            }
        }
        protected static object Default(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
        public abstract object Read(TContext context, IRepPrimitive value);
        public abstract object Read(TContext context, IRepCollection value);
        public abstract object Read(TContext context, IRepObject value);
    }
}
