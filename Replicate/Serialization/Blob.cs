using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization {
    [DebuggerDisplay("{String}")]
    public class Blob {
        byte[] Bytes;
        public Stream Stream { get => new MemoryStream(Bytes); }
        public virtual void SetStream(Stream stream) {
            Bytes = new byte[(int)stream.Length];
            stream.Read(Bytes, 0, Bytes.Length);
        }
        public Blob(Stream stream) => SetStream(stream);
        public Blob(byte[] bytes) { Bytes = bytes; }
        public Blob(string str) : this(str == null ? null : Encoding.UTF8.GetBytes(str)) { }
        public static Blob FromString(string str) => new Blob(str);
        public string ReadString() => Bytes == null ? null : Encoding.UTF8.GetString(Bytes);
        public string String {
            get => ReadString();
            set => Bytes = value == null ? null : Encoding.UTF8.GetBytes(value);
        }
        public Blob() { }
    }
    public struct TypedBlob {
        [Replicate]
        public TypeId Type;
        [Replicate]
        public Blob Value;
        public static object ConvertTo(IReplicateSerializer serializer, object obj) {
            if (obj == null) return null;
            var type = obj.GetType();
            return new TypedBlob() {
                Type = serializer.Model.GetId(type),
                Value = new Blob(serializer.Serialize(type, obj))
            };
        }
        public static object ConvertFrom(IReplicateSerializer serializer, object obj) {
            if (obj == null || !(obj is TypedBlob blob)) return null;
            if (blob.Type.Id.IsEmpty) return null;
            var type = serializer.Model.GetType(blob.Type);
            return serializer.Deserialize(type, blob.Value.Stream);
        }
    }
}
