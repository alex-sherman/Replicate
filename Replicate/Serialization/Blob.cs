using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class Blob
    {
        public static Blob None { get; } = new Blob();
        public Type Type { get; protected set; }
        public object Value { get; protected set; }
        public Blob(object obj, Type type = null)
        {
            if (obj == null && type == null)
                throw new ArgumentNullException($"Must provide either {nameof(obj)} or {nameof(type)}");
            Type = type ?? obj.GetType();
            Value = obj;
        }
        public Blob() { }
        public virtual void SetWireValue(Type type, Stream wire, IReplicateSerializer ser)
        {
            Type = type;
            Value = ser.Deserialize(Type, wire, null);
        }
    }
    public class DeferredBlob : Blob
    {
        private Stream _wireData;
        public override void SetWireValue(Type type, Stream wire, IReplicateSerializer ser)
        {
            _wireData = wire;
        }
        public object ReadInto<T>(object existing, IReplicateSerializer ser)
        {
            if (_wireData == null) throw new InvalidOperationException("Must call SetWireValue first");
            return Value = ser.Deserialize(Type, _wireData, existing);
        }
    }
}
