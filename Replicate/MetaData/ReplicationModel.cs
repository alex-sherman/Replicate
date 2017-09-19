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
        Dictionary<Type, TypeData> typeLookup = new Dictionary<Type, TypeData>();
        Dictionary<string, TypeData> stringLookup = new Dictionary<string, TypeData>();
        public TypeData this[Type type]
        {
            get { return typeLookup[type]; }
        }
        public TypeData this[string typeName]
        {
            get { return stringLookup[typeName]; }
        }
        public TypeData Add(Type type)
        {
            var output = new TypeData(type);
            typeLookup.Add(type, output);
            stringLookup.Add(type.Name, output);
            return output;
        }
    }
}
