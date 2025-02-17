using NUnit.Framework;
using Replicate;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System.Linq;
using System.Text;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace ReplicateTest {
    [ReplicateType]
    public enum JSONEnum {
        One = 1,
        Two = 2,
        Three = 3,
    }
    [ReplicateType]
    public class PropClass {
        [Replicate(1)]
        public uint Property { get; set; }

        public override bool Equals(object obj) {
            return (obj is PropClass other) &&
                   Property.Equals(other.Property);
        }
    }
    [ReplicateType]
    public class SubClass : PropClass {
        [Replicate(2)]
        public string Field = "";

        public override bool Equals(object obj) {
            return (obj is SubClass other) &&
                   Property == other.Property &&
                   Field == other.Field;
        }
    }
    [ReplicateType]
    public class GenericClass<T> {
        [Replicate(1)]
        public T Value;
        [Replicate(2)]
        public T Prop { get; set; }

        public override bool Equals(object obj) {
            return (obj is GenericClass<T> other) &&
                   Value.Equals(other.Value) &&
                   Prop.Equals(other.Prop);
        }
    }
    [ReplicateType]
    public class GenericSubClass<T, V> : GenericClass<T> {
        [Replicate(3)]
        public V OtherValue;
    }
    [ReplicateType]
    public class ObjectWithDictField<K, V> {
        [Replicate(1)]
        public Dictionary<K, V> Dict;

        public override bool Equals(object obj) {
            return (obj is ObjectWithDictField<K, V> other) && Dict.SequenceEqual(other.Dict);
        }
    }
    [ReplicateType]
    public class Collection<T> {
        // TODO: Generic arrays don't work?
        //public T[] Array;
        [Replicate]
        public IEnumerable<T> IEnumerable;
        [Replicate]
        public ICollection<T> ICollection;
        [Replicate]
        public List<T> List;
        [Replicate]
        public HashSet<T> HashSet;
        public override bool Equals(object obj) {
            return (obj is Collection<T> other) &&
                //Array.SequenceEqual(other.Array) &&
                SerializerTest.SequenceEqual(IEnumerable, other.IEnumerable) &&
                SerializerTest.SequenceEqual(ICollection, other.ICollection) &&
                SerializerTest.SequenceEqual(List, other.List) &&
                SerializerTest.SequenceEqual(HashSet, other.HashSet);
        }
        public override string ToString() {
            List<string> members = new List<string>();
            if (IEnumerable != null) members.Add("IEnumerable");
            if (ICollection != null) members.Add("ICollection");
            if (List != null) members.Add("List");
            if (HashSet != null) members.Add("HashSet");
            return $"{{{string.Join("|", members)}}}>";
        }
    }
    public class SerializerTest {
        public static bool SequenceEqual<T>(IEnumerable<T> a, IEnumerable<T> b) {
            if (a == null) return b == null;
            return a.SequenceEqual<T>(b);
        }
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
