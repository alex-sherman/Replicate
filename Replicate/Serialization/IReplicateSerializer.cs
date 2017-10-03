using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    interface IReplicateSerializer
    {
        void Write(object obj, Stream stream);
        object Read(Stream stream);
    }
}
