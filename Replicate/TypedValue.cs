using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public struct TypedValue
    {
        public static TypedValue None { get; }
        public object Value;
        public TypedValue(object value)
        {
            Value = value;
        }
    }
    [Replicate]
    public class TypedValueSurrogate
    {
        [Replicate]
        public TypeID TypeID;
        [Replicate]
        public byte[] Value;
        public static implicit operator TypedValue(TypedValueSurrogate self)
        {
            var serializer = ReplicateContext.Current.Serializer;
            var model = serializer.Model;
            return new TypedValue(serializer.Deserialize(
                null, new MemoryStream(self.Value),
                model.GetTypeAccessor(model.GetType(self.TypeID)), null)
            );
        }
        public static implicit operator TypedValueSurrogate(TypedValue value)
        {
            var serializer = ReplicateContext.Current.Serializer;
            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, value.Value);
            return new TypedValueSurrogate()
            {
                TypeID = serializer.Model.GetID(value.Value.GetType()),
                Value = stream.ToArray()
            };
        }
    }
}
