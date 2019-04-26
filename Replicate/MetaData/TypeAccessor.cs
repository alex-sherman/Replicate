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
        public Dictionary<string, MemberAccessor> Members;
        public TypeData TypeData { get; private set; }
        public TypeAccessor Surrogate { get; private set; }
        public TypeAccessor(TypeData typeData, Type type)
        {
            if (typeData.Surrogate != null)
            {
                var surrogateType = typeData.Surrogate;
                // TODO: This is untested and maybe confusing?
                if (surrogateType.IsGenericTypeDefinition)
                    surrogateType = surrogateType.MakeGenericType(type.GetGenericArguments());

                Surrogate = typeData.Model.GetTypeAccessor(surrogateType);
            }
            TypeData = typeData;
            Type = type;
            Name = type.FullName;
        }
        internal void InitializeMembers()
        {
            MemberAccessors = TypeData.ReplicatedMembers
                .Select(member => new MemberAccessor(member, this, TypeData.Model))
                .ToArray();
            Members = MemberAccessors.ToDictionary(member => member.Info.Name);
        }
        public object Construct()
        {
            return Activator.CreateInstance(Type);
        }
        public object Coerce(object obj)
        {
            if (Type.IsEnum && obj is int intValue)
                return Enum.ToObject(Type, intValue);
            return Convert.ChangeType(obj, Type);
        }
    }
}
