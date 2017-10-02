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
        public object Read(BinaryReader stream)
        {
            return stream.ReadInt32();
        }

        public void Write(object obj, BinaryWriter stream)
        {
            stream.Write(Convert.ToInt32(obj));
        }
    }
    class FloatSerializer : IReplicateSerializer
    {
        public object Read(BinaryReader stream)
        {
            return stream.ReadSingle();
        }

        public void Write(object obj, BinaryWriter stream)
        {
            stream.Write((float)obj);
        }
    }
    class StringSerializer : IReplicateSerializer
    {
        public object Read(BinaryReader stream)
        {
            return stream.ReadString();
        }

        public void Write(object obj, BinaryWriter stream)
        {
            stream.Write((string)obj);
        }
    }
}
