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

namespace Replicate.MetaData {
    public class TypeAccessor {
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public Type Type { get; private set; }
        public RepSet<MethodInfo> Methods;
        public RepSet<MemberAccessor> Members;
        public RepSet<MemberAccessor> SerializedMembers;
        private bool IsStringDict {
            get {
                if (!Type.IsSameGeneric(typeof(Dictionary<,>))) return false;
                var keyType = Type.GetGenericArguments()[0];
                if (keyType == typeof(string)
                    || TypeData.Model[keyType].Surrogate?.GetSurrogateType(keyType) == typeof(string))
                    return true;
                return false;
            }
        }
        public bool IsDictObj => IsStringDict && TypeData.Model.DictionaryAsObject;
        public readonly bool IsNullable;
        public readonly bool IsTypeless = false;
        public TypeData TypeData { get; private set; }
        public SurrogateAccessor Surrogate { get; private set; }
        public TypeAccessor(TypeData typeData, Type type) {
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
        }
        internal void InitializeMembers() {
            Members = TypeData.Members
                .Select(kvp => new KeyValuePair<RepKey, MemberAccessor>(kvp.Key, new MemberAccessor(kvp.Value, this, TypeData.Model)))
                .ToRepSet();
            SerializedMembers = Members
                .Where(m => m.Value.Info.GetAttribute<NonSerializedAttribute>() == null)
                .Where(m => m.Value.CanRead)
                .ToRepSet();
            // TODO: Actually handle generic RPC methods
            Methods = TypeData.Methods
                .Select((m, i) => new KeyValuePair<RepKey, MethodInfo>(new RepKey(i, m.Name), m)).ToRepSet();
        }
        public object Construct(params object[] args) {
            if (TypeData.MarshallMethod == MarshallMethod.None) return null;
            return Activator.CreateInstance(Type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        public MemberAccessor this[RepKey key] => Members[key];
        public override string ToString() {
            return TypeData.ToString();
        }
    }
}
