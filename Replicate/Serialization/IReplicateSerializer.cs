using Replicate.MetaData;
using System;
using System.IO;

namespace Replicate.Serialization {
    public interface IReplicateSerializer {
        ReplicationModel Model { get; }
        Stream Serialize(Type type, object obj, Stream stream);
        object Deserialize(Type type, Stream wireValue, object existing = null);
        T Deserialize<T>(Stream wireValue);
    }
    public static class SerializerExtensions {
        public static Stream Serialize<T>(this IReplicateSerializer ser, T obj, Stream stream) => ser.Serialize(typeof(T), obj, stream);
        public static Stream Serialize<T>(this IReplicateSerializer ser, T obj) => ser.Serialize(typeof(T), obj);
        public static Stream Serialize(this IReplicateSerializer ser, Type type, object obj) {

            Stream stream = new MemoryStream();
            stream = ser.Serialize(type, obj, stream);
            stream.Position = 0;
            return stream;
        }
        public static string SerializeString<T>(this IReplicateSerializer ser, T obj) => new StreamReader(ser.Serialize(obj)).ReadToEnd();
        public static string SerializeString(this IReplicateSerializer ser, Type type, object obj) => new StreamReader(ser.Serialize(type, obj)).ReadToEnd();
        public static byte[] SerializeBytes<T>(this IReplicateSerializer ser, T obj) => GetBytes(ser.Serialize(obj));
        public static byte[] SerializeBytes(this IReplicateSerializer ser, Type type, object obj) => GetBytes(ser.Serialize(type, obj));
        public static object Deserialize(this IReplicateSerializer ser, Type type, string str, object existing = null) => ser.Deserialize(type, GetStream(str), existing);
        public static T Deserialize<T>(this IReplicateSerializer ser, string str) => ser.Deserialize<T>(GetStream(str));
        public static object Deserialize(this IReplicateSerializer ser, Type type, byte[] bytes, object existing = null) => ser.Deserialize(type, new MemoryStream(bytes), existing);
        public static T Deserialize<T>(this IReplicateSerializer ser, byte[] bytes) => ser.Deserialize<T>(new MemoryStream(bytes));

        public static Stream GetStream(string wireValue) {
            var stream = new MemoryStream();
            if (wireValue != null) {
                var sw = new StreamWriter(stream);
                sw.Write(wireValue);
                sw.Flush();
                stream.Position = 0;
            }
            return stream;
        }
        public static byte[] GetBytes(Stream stream) {
            var output = new byte[(int)stream.Length];
            stream.Read(output, 0, output.Length);
            return output;
        }
    }
    public interface ITypedSerializer {
        void Write(object obj, Stream stream);
        object Read(Stream stream);
    }
}
