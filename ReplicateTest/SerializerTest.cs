using NUnit.Framework;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplicateTest {
    public class SerializerTest {
        public static Args Case<T>(T obj, string str) {
            return new Args() { Object = obj, Type = typeof(T), String = str };
        }
        public static Args Case<T>(T obj, params byte[] serialized) {
            return new Args() { Object = obj, Type = typeof(T), Serialized = serialized };
        }
        public static Args Case<T>(T obj) {
            return new Args() { Object = obj, Type = typeof(T) };
        }
        public class Args {
            public object Object;
            public Type Type;
            public byte[] Serialized;
            public string String;
            public override string ToString() { return $"{Type.Name}:{Object}"; }
            public void SerDes(IReplicateSerializer ser) {
                var bytes = ser.SerializeBytes(Type, Object);
                // string.Join(", ", bytes.Select(b => $"0x{b:X2}")) in immediate window for cheating
                if (Serialized != null)
                    CollectionAssert.AreEqual(Serialized, bytes);
                if (String != null)
                    CollectionAssert.AreEqual(String, Encoding.UTF8.GetString(bytes));
                var output = ser.Deserialize(Type, bytes);
                Assert.AreEqual(Object, output);
            }
            public static IEnumerable<Args> Combine(Args[] args, IEnumerable<string> serialized) {
                return args.Zip(serialized.Concat(Enumerable.Repeat<string>(null, args.Length)),
                    (a, s) => new Args() {
                        Object = a.Object,
                        Type = a.Type,
                        String = s,
                    });
            }
        }
    }
}
