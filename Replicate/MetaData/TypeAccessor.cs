using Replicate.Messages;
using Replicate.MetaData;
using Replicate.MetaData.Policy;
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
        public string FullName { get; private set; }
        public Type Type { get; private set; }
        public RepSet<MethodInfo> Methods;
        public RepSet<MemberAccessor> Members;
        public RepSet<MemberAccessor> StaticMembers;
        private readonly bool isStringDict;
        public bool IsDictObj => isStringDict && TypeData.Model.DictionaryAsObject;
        public readonly bool IsNullable;
        public readonly bool IsTypeless = false;
        public TypeData TypeData { get; private set; }
        public SurrogateAccessor Surrogate { get; private set; }
        public TypeAccessor(TypeData typeData, Type type)
        {

            TypeData = typeData;
            Type = type;
            if (Type.IsSameGeneric(typeof(Nullable<>))) Type = Type.GetGenericArguments()[0];
            IsTypeless = type == typeof(IRepNode);
            Name = type.Name;
            FullName = type.FullName;
            IsNullable = (type.IsSameGeneric(typeof(Nullable<>)) || type.IsClass || type == typeof(string))
                && type.GetCustomAttribute(typeof(NonNullAttribute)) == null;
            if (typeData.Surrogate != null)
                Surrogate = new SurrogateAccessor(this, typeData.Surrogate, typeData.Model);
            isStringDict = Type.IsSameGeneric(typeof(Dictionary<,>))
                && Type.GetGenericArguments()[0] == typeof(string);
        }
        internal void InitializeMembers()
        {
            Members = TypeData.Members
                .Select(kvp => new KeyValuePair<RepKey, MemberAccessor>(kvp.Key, new MemberAccessor(kvp.Value, this, TypeData.Model)))
                .ToRepSet();
            StaticMembers = TypeData.StaticMembers
                .Select(kvp => new KeyValuePair<RepKey, MemberAccessor>(kvp.Key, new MemberAccessor(kvp.Value, this, TypeData.Model)))
                .ToRepSet();
            // TODO: Actually handle generic RPC methods
            Methods = TypeData.Methods
                .Select((m, i) => new KeyValuePair<RepKey, MethodInfo>(new RepKey(i, m.Name), m)).ToRepSet();
        }
        public object Construct(params object[] args)
        {
            if (TypeData.MarshallMethod == MarshallMethod.None) return null;
            return Activator.CreateInstance(Type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        public MemberAccessor this[RepKey key] => Members[key];
        public override string ToString()
        {
            return TypeData.ToString();
        }
    }
}
