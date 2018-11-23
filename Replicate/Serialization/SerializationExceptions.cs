using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class SerializationError : Exception
    {
        public SerializationError(string message) : base(message) { }
        public SerializationError() : base() { }
    }
}
