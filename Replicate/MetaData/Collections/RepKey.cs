using System.Collections.Generic;

namespace Replicate.MetaData {
    [ReplicateType]
    public struct RepKey {
        public int? Index;
        public string Name;
        public RepKey(int index, string str) { Index = index; Name = str; }

        public override bool Equals(object obj) {
            if (!(obj is RepKey)) return false;

            var key = (RepKey)obj;
            return EqualityComparer<int?>.Default.Equals(Index, key.Index) && Name == key.Name;
        }

        public override int GetHashCode() {
            var hashCode = -1868479479;
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public static implicit operator RepKey(string str) => new RepKey() { Name = str };
        public static implicit operator RepKey(int index) => new RepKey() { Index = index };
        [ReplicateIgnore]
        public bool IsEmpty => Name == null && Index == null;
        [ReplicateIgnore]
        public bool IsFull => Name != null && Index != null;
        public override string ToString() {
            if (Name != null) {
                if (Index != null) return $"{Index}:{Name}";
                return Name;
            }
            if (Index != null) return Index.ToString();
            return "<Empty>";
        }
    }
}
