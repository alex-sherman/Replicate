using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    [ReplicateType]
    public class MemberDescription
    {
        public RepKey Key;
        public RepKey TypeId;
        public byte? GenericPosition;
    }
    [ReplicateType]
    public class TypeDescription
    {
        public RepKey Key;
        public bool IsFake;
        public string[] GenericParameters;
        public List<MemberDescription> Members;
    }
    [ReplicateType]
    public class ModelDescription
    {
        public List<TypeDescription> Types;
    }
}
