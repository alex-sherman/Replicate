using Replicate.Messages;
using Replicate.MetaTyping;
using Replicate.Serialization;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public enum MarshallMethod
    {
        None = 0,
        Primitive = 1,
        Object = 2,
        Collection = 3,
        Blob = 4,
    }
    public enum PrimitiveType
    {
        None = 0,
        Bool = 1,
        Byte = 2,
        VarInt = 4,
        SVarInt = 5,
        Float = 6,
        Double = 7,
        String = 8,
    }
    public static class PrimitiveTypeMap
    {
        static Dictionary<Type, PrimitiveType> Map = new Dictionary<Type, PrimitiveType>()
        {
            { typeof(bool),   PrimitiveType.Bool },
            { typeof(byte),   PrimitiveType.Byte },
            { typeof(char),   PrimitiveType.Byte },
            { typeof(short),  PrimitiveType.SVarInt },
            { typeof(ushort), PrimitiveType.VarInt },
            { typeof(int),    PrimitiveType.SVarInt },
            { typeof(uint),   PrimitiveType.VarInt },
            { typeof(long),   PrimitiveType.SVarInt },
            { typeof(ulong),  PrimitiveType.VarInt },
            { typeof(float),  PrimitiveType.Float },
            { typeof(double), PrimitiveType.Double },
            { typeof(string), PrimitiveType.String },
        };
        public static PrimitiveType MapType(Type type)
        {
            if (type.IsEnum)
                return PrimitiveType.VarInt;
            return Map[type];
        }
        public static bool Contains(Type type)
        {
            return type.IsEnum || Map.ContainsKey(type);
        }
    }
    public class ReplicationModel : IEnumerable<TypeData>
    {
        private static ReplicationModel @default;
        public static ReplicationModel Default => @default ?? (@default = new ReplicationModel());
        ConcurrentDictionary<Type, TypeAccessor> typeAccessorLookup = new ConcurrentDictionary<Type, TypeAccessor>();
        ConcurrentDictionary<Type, TypeData> typeLookup = new ConcurrentDictionary<Type, TypeData>();
        public readonly RepSet<TypeData> Types = new RepSet<TypeData>();
        internal HashSet<Type> SurrogateTypes = new HashSet<Type>();
        public readonly ModuleBuilder Builder = DynamicModule.Create();
        public bool DictionaryAsObject = true;
        public bool AddOnLookup = false;
        public Func<TypeAccessor, object, object> Coerce = DefaultCoercion;
        public bool Frozen { get; set; } = false;
        public static object DefaultCoercion(TypeAccessor dest, object value)
        {
            if (value == null || dest.TypeData.MarshallMethod == MarshallMethod.None) return value;
            if (dest.Type.IsEnum)
            {
                if (value is string str) {
                    if (string.IsNullOrEmpty(str)) return null;
                    return Enum.Parse(dest.Type, str);
                }
                return Enum.ToObject(dest.Type, Convert.ChangeType(value, typeof(int)));
            }
            return Convert.ChangeType(value, dest.Type);
        }
        public ReplicationModel(bool loadTypes = true, bool addBaseTypes = true)
        {
            if (addBaseTypes) AddBaseTypes();
            if (loadTypes) LoadTypes(Assembly.GetCallingAssembly());
        }
        private void AddBaseTypes()
        {
            Add(typeof(None));
            Add(typeof(Dictionary<,>));
            Add(typeof(List<>));
            Add(typeof(ICollection<>));
            Add(typeof(IEnumerable<>));
            Add(typeof(byte));
            Add(typeof(short));
            Add(typeof(int));
            Add(typeof(long));
            Add(typeof(ushort));
            Add(typeof(uint));
            Add(typeof(ulong));
            Add(typeof(float));
            Add(typeof(double));
            Add(typeof(string));
            Add(typeof(Guid)).SetSurrogate(new Surrogate(typeof(byte[]),
                (_, __) => (_, obj) => obj == null ? null : ((Guid)obj).ToByteArray(),
                (_, __) => (_, obj) =>
                {
                    if (obj == null) return null;
                    return new Guid((byte[])(obj));
                }));
            Add(typeof(Blob));
            Add(typeof(TypedBlob));
            Add(typeof(object)).SetSurrogate(new Surrogate(typeof(TypedBlob),
                (_, __) => TypedBlob.ConvertTo,
                (_, __) => TypedBlob.ConvertFrom
            ));
            var repNodeTypeData = Add(typeof(IRepNode));
            repNodeTypeData.MarshallMethod = MarshallMethod.None;
            Add(typeof(RepKeyValuePair<,>));
            var kvpTD = Add(typeof(KeyValuePair<,>));
            kvpTD.SetSurrogate(new Surrogate(typeof(RepKeyValuePair<,>)));
            LoadTypes();
        }

        public IRepNode GetRepNode(object backing, Type type) => GetRepNode(backing, GetTypeAccessor(type), null);
        public IRepNode GetRepNode(object backing, TypeAccessor typeAccessor, MemberAccessor memberAccessor)
        {
            IRepNode output;
            if (backing == null && typeAccessor == null)
                throw new InvalidOperationException($"Must provide either {nameof(backing)} or {nameof(typeAccessor)}");
            typeAccessor = typeAccessor ?? GetTypeAccessor(backing.GetType());
            if (typeAccessor.IsTypeless)
            {
                if (backing == null)
                    output = new RepNodeTypeless(this, memberAccessor);
                else
                    output = (IRepNode)backing;
            }
            else if (DictionaryAsObject && typeAccessor.Type.IsSameGeneric(typeof(Dictionary<,>))
                && typeAccessor.Type.GetGenericArguments()[0] == typeof(string))
            {
                var childType = typeAccessor.Type.GetGenericArguments()[1];
                var dictObjType = typeof(RepDictObject<>).MakeGenericType(childType);
                output = (IRepNode)Activator.CreateInstance(dictObjType, backing, typeAccessor, memberAccessor, this);
            }
            else
                output = new RepBackedNode(backing, typeAccessor, memberAccessor, this);
            return output;
        }

        public TypeId GetId(Type type)
        {
            var typeData = this[type];
            // This might be only a subset of such resolutions, possibly also do for subclasses?
            if (typeData.Type.IsInterface && !type.IsSameGeneric(typeData.Type))
                type = type.GetInterfaces().First(i => i.IsSameGeneric(typeData.Type));
            var output = new TypeId()
            {
                Id = Types.GetKey(typeData.FullName),
            };
            if (type.IsGenericType)
                output.Subtypes = type.GetGenericArguments().Select(t => GetId(t)).ToArray();
            return output;
        }
        public Type GetType(TypeId typeID)
        {
            Type type = Types[typeID.Id].Type;
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(typeID.Subtypes.Select(subType => GetType(subType)).ToArray());
            }
            return type;
        }
        public MethodKey MethodKey(MethodInfo method)
        {
            var type = method.DeclaringType;
            var methods = GetTypeAccessor(method.DeclaringType).Methods;
            if (!methods.ContainsKey(method.Name)) throw new ContractNotFoundError(method.Name);
            return new MethodKey() { Method = methods.GetKey(method.Name), Type = GetId(type) };
        }
        public MethodInfo GetMethod(MethodKey method)
        {
            var methods = GetTypeAccessor(GetType(method.Type)).Methods;
            if (!methods.ContainsKey(method.Method)) throw new ContractNotFoundError(method.Method.ToString());
            return methods[method.Method];
        }
        public void ClearTypeAccessorCache() => typeAccessorLookup.Clear();
        public TypeAccessor GetTypeAccessor(Type type)
        {
            lock (typeAccessorLookup)
            {
                if (type.IsGenericTypeDefinition)
                    throw new InvalidOperationException("Cannot create a type accessor for a generic type definition");
                if (!typeAccessorLookup.TryGetValue(type, out TypeAccessor typeAccessor))
                {
                    var internalType = type;
                    if (internalType.IsSameGeneric(typeof(Nullable<>))) internalType = type.GetGenericArguments()[0];
                    typeAccessor = typeAccessorLookup[type] = new TypeAccessor(GetTypeData(internalType), type);
                    typeAccessor.InitializeMembers();
                }
                return typeAccessor;
            }
        }
        public TypeAccessor GetCollectionValueAccessor(Type collectionType)
        {
            Type interfacedCollectionType = null;
            if (collectionType.IsSameGeneric(typeof(IEnumerable<>)))
            {
                interfacedCollectionType = collectionType;
            }
            else if (collectionType.Implements(typeof(IEnumerable<>)))
            {
                if (collectionType.GetInterface("ICollection`1") != null)
                    interfacedCollectionType = collectionType.GetInterface("ICollection`1");
                else if (collectionType.GetInterface("IEnumerable`1") != null)
                    interfacedCollectionType = collectionType.GetInterface("IEnumerable`1");
            }
            if (interfacedCollectionType == null)
                throw new InvalidOperationException($"{collectionType.FullName} is not a valid collection type");
            return GetTypeAccessor(interfacedCollectionType.GetGenericArguments()[0]);
        }
        // Will never add to typeIndex, but may add a reference in typeLookup to an existing typeData in typeIndex
        public bool TryGetTypeData(Type type, out TypeData typeData)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            if (typeLookup.TryGetValue(type, out typeData))
                return true;
            if (type.Implements(typeof(IRepNode)))
            {
                typeData = ResolveFrom(type, typeof(IRepNode));
                return true;
            }
            if (type.Implements(typeof(IEnumerable<>)))
            {
                typeData = ResolveFrom(type, type.Implements(typeof(ICollection<>)) ? typeof(ICollection<>) : typeof(IEnumerable<>));
                return true;
            }
            return false;
        }
        private TypeData ResolveFrom(Type incoming, Type existing)
        {
            var typeData = this[existing];
            typeLookup.TryAdd(incoming, typeData);
            return typeData;
        }
        public TypeData GetTypeData(Type type)
        {
            if (TryGetTypeData(type, out var typeData)) return typeData;
            if (AddOnLookup) return Add(type);
            throw new KeyNotFoundException(string.Format("The type {0} has not been added to the replication model", type.FullName)) { Source = type.FullName };
        }
        public TypeData this[Type type]
        {
            get { return GetTypeData(type); }
        }
        public TypeData Add(Type type, ReplicateTypeAttribute attr = null, bool addMembers = true)
        {
            if (type.IsNotPublic)
                throw new InvalidOperationException("Cannot add a non public type to the replication model");
            IEnumerable<Type> genericParameters = null;
            if (type.IsGenericType)
            {
                genericParameters = type.GetGenericArguments()
                    .SelectMany(t => t.IsGenericType ? t.GetGenericArguments() : new[] { t })
                    .Where(t => !t.IsGenericParameter);
                type = type.GetGenericTypeDefinition();
            }
            if (!typeLookup.TryGetValue(type, out var typeData))
            {
                if (Frozen) throw new InvalidOperationException($"Attempted to add a type to a frozen model");
                typeData = new TypeData(type, this) { TypeAttribute = attr };
                typeLookup.TryAdd(type, typeData);
                if (Types.ContainsKey(typeData.FullName))
                    throw new ArgumentException($"Failed to add type {type.FullName}, a type with that name already exists");
                var key = Types.Add(typeData.FullName, typeData);
                if (typeData.FullName != typeData.Name && !Types.ContainsKey(typeData.Name))
                    Types.AddAlias(key, typeData.Name, typeData);
                if (addMembers) typeData.InitializeMembers();
            }
            if (genericParameters != null)
                foreach (var generic in genericParameters)
                    Add(generic);
            return typeData;
        }

        public ModelDescription GetDescription()
        {
            return new ModelDescription()
            {
                Types = Types.Values.Select(typeData => new TypeDescription()
                {
                    Key = Types.GetKey(typeData.FullName),
                    GenericParameters = typeData.GenericTypeParameters?.ToArray(),
                    Members = typeData.Keys.Select(key =>
                    {
                        var member = typeData[key];
                        var desc = new MemberDescription()
                        {
                            Key = key,
                        };
                        if (member.IsGenericParameter)
                            desc.GenericPosition = (byte)member.GenericParameterPosition;
                        else
                            desc.TypeId = Types.GetKey(typeData.FullName);
                        return desc;
                    }).ToList(),
                    IsFake = typeData.Type.GetCustomAttribute<FakeTypeAttribute>() != null,
                }).ToList()
            };
        }
        private Type FindType(TypeDescription type, IEnumerable<Assembly> assemblies, List<(TypeDescription, Fake)> deferred, bool fakeMissing)
        {
            var typeName = type.Key.Name;
            if (!type.IsFake)
            {
                var found = Type.GetType(typeName)
                    ?? assemblies.Select(a => a.GetType(typeName)).Where(t => t != null).FirstOrDefault();
                if (found != null) return found;
            }
            else
            {
                var found = Builder.GetType(type.Key.Name);
                if (found != null) return found;
            }
            if (type.IsFake || fakeMissing)
            {
                var fake = new Fake(type.Key.Name, Builder);
                if (type.GenericParameters != null)
                    fake.MakeGeneric(type.GenericParameters.ToArray());
                deferred.Add((type, fake));
                return fake.IntermediateType;
            }
            throw new TypeLoadException($"Unable to load type {type.Key}");
        }
        public void LoadFrom(ModelDescription description, bool fakeMissing = true)
        {
            typeLookup.Clear();
            Types.Clear();
            var assemblies = new[] { Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly() };
            var deferred = new List<(TypeDescription, Fake)>();
            foreach (var typeDesc in description.Types)
            {
                var type = FindType(typeDesc, assemblies, deferred, fakeMissing);
                Types[typeDesc.Key] = new TypeData(type, this);
            }
            foreach (var (typeDesc, fake) in deferred)
            {
                foreach (var member in typeDesc.Members)
                {
                    if (member.GenericPosition != null)
                        fake.AddField(member.GenericPosition.Value, member.Key.Name);
                    else
                        fake.AddField(Types[member.TypeId].Type, member.Key.Name);
                }
                Types[typeDesc.Key] = new TypeData(fake.Build(), this);
            }
            foreach (var td in Types.Values)
            {
                typeLookup[td.Type] = td;
            }
            foreach (var type in Types.Values)
            {
                type.InitializeMembers();
            }
        }

        public void LoadTypes(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();
            foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttribute<ReplicateTypeAttribute>() != null))
                Add(type);
        }

        public IEnumerator<TypeData> GetEnumerator() => typeLookup.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
