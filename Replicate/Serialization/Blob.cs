using System;
using System.Collections.Generic;
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
        public virtual void SetWireValue<T>(Type type, object wire, IReplicateSerializer<T> ser)
        {
            Type = type;
            Value = ser.Deserialize(Type, (T)wire, null);
        }
    }
    public class DeferredBlob : Blob
    {
        private object _wireData;
        public override void SetWireValue<T>(Type type, object wire, IReplicateSerializer<T> ser)
        {
            _wireData = wire;
        }
        public object ReadInto<T>(object existing, IReplicateSerializer<T> ser)
        {
            if (_wireData == null) throw new InvalidOperationException("Must call SetWireValue first");
            if (!(_wireData is T wire)) throw new InvalidCastException();
            return Value = ser.Deserialize(Type, wire, existing);
        }
    }
}
