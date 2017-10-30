using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [Replicate]
    public struct TypeID
    {
        [Replicate]
        public ushort id;
        [Replicate]
        public TypeID[] subtypes;
    }
}
