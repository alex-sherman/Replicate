using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class TypeAccessor
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public MemberAccessor[] MemberAccessors;
        public TypeData TypeData { get; private set; }
        public TypeAccessor(TypeData typeData, Type type)
        {
            TypeData = typeData;
            Type = type;
            Name = type.FullName;
            MemberAccessors = typeData.ReplicatedMembers
                .Select(member => new MemberAccessor(member, this))
                .ToArray();
        }
        public object Construct()
        {
            var cinfo = Type.GetConstructor(new Type[] { });
            return cinfo.Invoke(new object[] { });
        }
    }
}
