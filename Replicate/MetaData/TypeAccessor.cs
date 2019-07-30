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
        private readonly bool isStringDict;
        public bool IsDictObj => isStringDict && TypeData.Model.DictionaryAsObject;
        public bool IsTypeless = false;
        public TypeData TypeData { get; private set; }
        public SurrogateAccessor Surrogate { get; private set; }
        public TypeAccessor(TypeData typeData, Type type)
        {
            TypeData = typeData;
            Type = type;
            IsTypeless = type == typeof(IRepNode);
            Name = type.FullName;
            if (typeData.Surrogate != null)
                Surrogate = new SurrogateAccessor(this, typeData.Surrogate, typeData.Model);
            isEnum = Type.IsEnum;
            isStringDict = Type.IsSameGeneric(typeof(Dictionary<,>))
                && Type.GetGenericArguments()[0] == typeof(string);
        }
        internal void InitializeMembers()
        {
            MemberAccessors = TypeData.ReplicatedMembers
                .Select(member => new MemberAccessor(member, this, TypeData.Model))
                .ToArray();
            Members = MemberAccessors.ToDictionary(member => member.Info.Name);
        }
        public object Construct(params object[] args)
        {
            if (TypeData.MarshallMethod == MarshallMethod.None) return null;
            return Activator.CreateInstance(Type, args);
        }
        private bool isEnum;
        public object Coerce(object obj)
        {
            if (obj == null || TypeData.MarshallMethod == MarshallMethod.None) return obj;
            if (isEnum && obj is int intValue)
                return Enum.ToObject(Type, intValue);
            return Convert.ChangeType(obj, Type);
        }
        public IEnumerable<MemberKey> Keys => MemberAccessors.Select((m, i) => new MemberKey() { Index = i, Name = m.Info.Name });
        public MemberAccessor this[MemberKey key]
            => key.Index.HasValue
                ? MemberAccessors[key.Index.Value]
                : Members.TryGetValue(key.Name, out var member) ? member : null;
    }
}
