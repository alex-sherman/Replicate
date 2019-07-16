using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    [ReplicateType]
    public class TypeDescription
    {
        public string Name;
        public string FakeSourceType;
        public List<string> Members;
    }
    [ReplicateType]
    public class ModelDescription
    {
        public List<TypeDescription> Types;
    }
}
