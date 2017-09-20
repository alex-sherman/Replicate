using ProtoBuf;
using ProtoBuf.Meta;
using Replicate.Messages;
using System;
using System.Collections.Generic;
using System.IO;
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
            get { if (typeLookup.ContainsKey(type)) return typeLookup[type]; return null; }
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
        public void Compile()
        {
            foreach (var typeData in typeLookup.Values)
            {
                foreach (var member in typeData.ReplicatedMembers)
                {
                    member.TypeData = this[member.MemberType];
                }
            }
        }
        public byte[] GetBytes(object obj, TypeData typeData)
        {

            MemoryStream stream = new MemoryStream();
            if (typeData == null)
                Serializer.NonGeneric.Serialize(stream, obj);
            else
            {
                if (obj != null)
                {
                    /// TODO: Replicate members by reference or copy depending on <see cref="ReplicationPolicy"/>
                    Serializer.Serialize(stream,
                        typeData.ReplicatedMembers
                        .Select((member, id) => new MemberData()
                        {
                            id = id,
                            value = GetBytes(member.GetValue(obj), member.TypeData)
                        }).ToList()
                    );
                }
            }
            return stream.ToArray();
        }
        public object FromBytes(object obj, byte[] bytes, Type type, TypeData typeData)
        {
            MemoryStream stream = new MemoryStream(bytes);
            if (typeData == null)
                return RuntimeTypeModel.Default.Deserialize(stream, obj, type);
            if (obj == null)
                obj = Activator.CreateInstance(type);
            foreach (var memberData in Serializer.Deserialize<List<MemberData>>(stream))
            {
                var member = typeData.ReplicatedMembers[memberData.id];
                member.SetValue(obj, FromBytes(member.GetValue(obj), memberData.value, member.MemberType, member.TypeData));
            }
            return obj;
        }
    }
}
