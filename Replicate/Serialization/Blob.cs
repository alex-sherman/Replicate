using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class Blob
    {
        public Stream Stream { get; protected set; }
        public virtual void SetStream(Stream stream)
        {
            Stream = stream;
        }
        public Blob(Stream stream) { SetStream(stream); }
        public static Blob FromString(string str) => str == null ? null : new Blob(new MemoryStream(Encoding.UTF8.GetBytes(str)));
        public string ReadString() {
            long position = Stream.Position;
            var str = Stream.ReadAllString();
            Stream.Position = position;
            return str;
        }
        public Blob() { }
    }
    public struct TypedBlob
    {
        [Replicate]
        public TypeId Type;
        [Replicate]
        public Blob Value;
        public static object ConvertTo(IReplicateSerializer serializer, object obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            return new TypedBlob()
            {
                Type = serializer.Model.GetId(type),
                Value = new Blob(serializer.Serialize(type, obj))
            };
        }
        public static object ConvertFrom(IReplicateSerializer serializer, object obj)
        {
            if (obj == null || !(obj is TypedBlob blob)) return null;
            if (blob.Type.Id.IsEmpty) return null;
            var type = serializer.Model.GetType(blob.Type);
            return serializer.Deserialize(type, blob.Value.Stream);
        }
    }
}
