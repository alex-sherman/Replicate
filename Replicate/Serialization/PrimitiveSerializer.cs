using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    class IntSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadInt32();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteInt32(Convert.ToInt32(obj));
        }
    }
    class FloatSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadSingle();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteSingle((float)obj);
        }
    }
    class StringSerializer : IReplicateSerializer
    {
        public object Read(Stream stream)
        {
            return stream.ReadString();
        }

        public void Write(object obj, Stream stream)
        {
            stream.WriteString((string)obj);
        }
    }
}
