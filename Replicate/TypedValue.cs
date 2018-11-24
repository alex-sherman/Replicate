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
}
