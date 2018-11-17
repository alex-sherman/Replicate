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
        public object value;
        public TypedValue(object value)
        {
            this.value = value;
        }
    }
    [Replicate]
    public class TypedValueSurrogate
    {
        [Replicate]
        public TypeID typeID;
        [Replicate]
        public byte[] value;
        public static implicit operator TypedValue(TypedValueSurrogate self)
        {
            var serializer = ReplicateContext.Current.Serializer;
            var model = serializer.Model;
            return new TypedValue(serializer.Deserialize(
                null, new MemoryStream(self.value),
                model.GetTypeAccessor(model.GetType(self.typeID)), null)
            );
        }
        public static implicit operator TypedValueSurrogate(TypedValue value)
        {
            var serializer = ReplicateContext.Current.Serializer;
            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, value.value);
            return new TypedValueSurrogate()
            {
                typeID = serializer.Model.GetID(value.value.GetType()),
                value = stream.ToArray()
            };
        }
    }
}
