using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public interface IReplicateSerializer<TWireType>
    {
        TWireType Serialize(Type type, object obj);
        object Deserialize(Type type, TWireType message);
    }
    public static class SerializerExtensions
    {
        public static TWireType Serialize<TWireType, T>(this IReplicateSerializer<TWireType> serializer, T value)
        {
            return serializer.Serialize(typeof(T), value);
        }
        public static T Deserialize<TWireType, T>(this IReplicateSerializer<TWireType> serializer, TWireType wire)
        {
            return (T)serializer.Deserialize(typeof(T), wire);
        }
    }
    public interface ITypedSerializer
    {
        void Write(object obj, Stream stream);
        object Read(Stream stream);
    }
}
