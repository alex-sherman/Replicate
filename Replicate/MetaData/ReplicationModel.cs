using Replicate.Messages;
using Replicate.MetaTyping;
using Replicate.Serialization;
using System;
using System.Collections;
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
            { typeof(short),  PrimitiveType.VarInt },
            { typeof(ushort), PrimitiveType.VarInt },
            { typeof(int),    PrimitiveType.VarInt },
            { typeof(uint),   PrimitiveType.VarInt },
            { typeof(long),   PrimitiveType.VarInt },
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
    }
    public class ReplicationModel : IEnumerable<TypeData>
    {
        public static ReplicationModel Default { get; } = new ReplicationModel();
        Dictionary<Type, TypeAccessor> typeAccessorLookup = new Dictionary<Type, TypeAccessor>();
        Dictionary<Type, TypeData> typeLookup = new Dictionary<Type, TypeData>();
        RepSet<TypeData> types = new RepSet<TypeData>();
        public readonly ModuleBuilder Builder = DynamicModule.Create();
        public bool DictionaryAsObject;
        public bool AddOnLookup = false;
        public ReplicationModel(bool loadTypes = true, bool addBaseTypes = true)
        {
            if (addBaseTypes) AddBaseTypes();
            if (loadTypes) LoadTypes(Assembly.GetCallingAssembly());
        }
        private void AddBaseTypes()
        {
            Add(typeof(None));
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
            Add(typeof(Dictionary<,>));
            Add(typeof(List<>));
            Add(typeof(ICollection<>));
            Add(typeof(IEnumerable<>));
            Add(typeof(Blob));
            Add(typeof(DeferredBlob));
            Add(typeof(object)).SetSurrogate(new Surrogate(typeof(Blob),
                (_, __) => obj => obj == null ? null : new Blob(obj),
                (_, __) => obj => obj == null ? null : ((Blob)obj).Value
            ));
            var repNodeTypeData = Add(typeof(IRepNode));
            repNodeTypeData.MarshallMethod = MarshallMethod.None;
            var kvpTD = Add(typeof(KeyValuePair<,>));
            kvpTD.MarshallMethod = MarshallMethod.Object;
            kvpTD.AddMember("Key");
            kvpTD.AddMember("Value");
            kvpTD.SetTupleSurrogate();
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
            var output = new TypeId()
            {
                Id = (ushort)types.GetKey(this[type].Name).Index.Value
            };
            if (type.IsGenericType)
                output.Subtypes = type.GetGenericArguments().Select(t => GetId(t)).ToArray();
            return output;
        }
        public Type GetType(TypeId typeID)
        {
            Type type = types[typeID.Id].Type;
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(typeID.Subtypes.Select(subType => GetType(subType)).ToArray());
            }
            return type;
        }
        public MethodKey MethodKey(MethodInfo method)
        {
            var type = method.DeclaringType;
            var methods = GetTypeAccessor(method.DeclaringType).RPCMethods;
            if (!methods.ContainsKey(method.Name)) throw new ContractNotFoundError(method.Name);
            return new MethodKey() { Method = methods.GetKey(method.Name), Type = GetId(type) };
        }
        public MethodInfo GetMethod(MethodKey method)
        {
            var methods = GetTypeAccessor(GetType(method.Type)).RPCMethods;
            if (!methods.ContainsKey(method.Method)) throw new ContractNotFoundError(method.Method.ToString());
            return methods[method.Method];
        }
        public TypeAccessor GetTypeAccessor(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new InvalidOperationException("Cannot create a type accessor for a generic type definition");
            if (!typeAccessorLookup.TryGetValue(type, out TypeAccessor typeAccessor))
            {
                if (type.IsSameGeneric(typeof(Nullable<>)))
                    type = type.GetGenericArguments()[0];
                typeAccessor = typeAccessorLookup[type] = new TypeAccessor(GetTypeData(type), type);
                typeAccessor.InitializeMembers();
            }
            return typeAccessor;
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
            typeLookup.Add(incoming, typeData);
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
        public TypeData Add(Type type)
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
                typeData = new TypeData(type, this);
                typeLookup.Add(type, typeData);
                types.Add(typeData.Name, typeData);
                typeData.InitializeMembers();
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
                Types = types.Values.Select(typeData => new TypeDescription()
                {
                    TypeId = (ushort)types.GetKey(typeData.Name).Index.Value,
                    Name = typeData.Name,
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
                            desc.TypeId = (ushort)types.GetKey(typeData.Name).Index.Value;
                        return desc;
                    }).ToList(),
                    FakeSourceType = typeData.Type.GetCustomAttribute<FakeTypeAttribute>()?.Source.FullName,
                }).ToList()
            };
        }
        public Type FindType(string typeName, IEnumerable<Assembly> assemblies, bool fakeMissing)
        {
            return Type.GetType(typeName)
                ?? assemblies.Select(a => a.GetType(typeName)).Where(t => t != null).FirstOrDefault()
                // TODO: Build fake instead
                //?? new Fake(typeName, Builder)
                ?? throw new TypeLoadException($"Unable to find type {typeName}");
        }
        public void LoadFrom(ModelDescription description, bool fakeMissing = true)
        {
            typeLookup.Clear();
            types.Clear();
            var assemblies = new[] { Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly() };
            foreach (var typeDesc in description.Types)
            {
                Type type;
                if (typeDesc.FakeSourceType != null)
                    type = Fake.FromType(FindType(typeDesc.FakeSourceType, assemblies, fakeMissing));
                else
                    type = FindType(typeDesc.Name, assemblies, fakeMissing);
                Add(type);
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
