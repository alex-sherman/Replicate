using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class MemberInfo
    {
        public string Name { get { return property?.Name ?? field?.Name; } }
        private FieldInfo field;
        private PropertyInfo property;
        public MemberInfo(FieldInfo field)
        {
            this.field = field;
        }
        public MemberInfo(PropertyInfo property)
        {
            this.property = property;
        }
        public Type MemberType { get { return property?.PropertyType ?? field?.FieldType; } }
        public ReplicationData TypeData { get; set; }

        public void SetValue(object obj, object value) { property?.SetValue(obj, value); field?.SetValue(obj, value); }
        public object GetValue(object obj) { return property?.GetValue(obj) ?? field?.GetValue(obj); }
    }
}
