using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class ReplicationModel
    {
        public static ReplicationModel Default { get; } = new ReplicationModel();
        Dictionary<Type, ReplicationData> typeLookup = new Dictionary<Type, ReplicationData>();
        public ReplicationData this[Type type]
        {
            get { return typeLookup[type]; }
        }
        public ReplicationData Add(Type type)
        {
            var output = new ReplicationData(type);
            typeLookup.Add(type, output);
            return output;
        }
    }
}
