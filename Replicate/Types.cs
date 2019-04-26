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
    public struct None { public static None Value { get; private set; } = new None(); }
    public class Ref<T> where T : struct
    {
        public Ref(T value) => Value = value;
        public T Value;
        public override string ToString() => Value.ToString();
        public static implicit operator T(Ref<T> wrapper) => wrapper.Value;
        public static implicit operator Ref<T>(T value) => new Ref<T>(value);
    }
}
