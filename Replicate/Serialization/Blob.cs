using Replicate.MetaData;
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
        public Stream Stream { get; protected set; }
        public virtual void SetStream(Stream stream)
        {
            Stream = stream;
        }
        public Blob(Stream stream) { SetStream(stream); }
        public Blob() { }
    }
    public struct TypedBlob
    {
        public TypeId Type;
        public Blob Value;
    }
}
