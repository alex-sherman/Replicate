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
            var manager = ReplicateContext.Current.Manager;
            var model = ReplicateContext.Current.Model;
            return new TypedValue(manager.Serializer.Deserialize(
                null, new MemoryStream(self.value),
                model.GetTypeAccessor(model.GetType(self.typeID)), null)
            );
        }
        public static implicit operator TypedValueSurrogate(TypedValue value)
        {
            var manager = ReplicateContext.Current.Manager;
            MemoryStream stream = new MemoryStream();
            manager.Serializer.Serialize(stream, value.value);
            return new TypedValueSurrogate()
            {
                typeID = ReplicateContext.Current.Model.GetID(value.value.GetType()),
                value = stream.ToArray()
            };
        }
    }
}
