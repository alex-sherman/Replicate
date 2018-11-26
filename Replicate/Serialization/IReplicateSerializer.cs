using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public interface IReplicateSerializer<TWireType>
    {
        TWireType Serialize(Type type, object obj);
        object Deserialize(Type type, TWireType message);
    }
    public interface ITypedSerializer
    {
        void Write(object obj, Stream stream);
        object Read(Stream stream);
    }
}
