using Replicate.MetaData.Policy;
using System.Collections.Generic;

namespace Replicate.MetaData {
    [ReplicateType]
    public struct TypeId {
        [Replicate(1)]
        public RepKey Id;
        [Replicate(2)]
        [SkipNull]
        public TypeId[] Subtypes;

        public override bool Equals(object obj) {
            if (!(obj is TypeId)) {
                return false;
            }

            var id = (TypeId)obj;
            return Id.Equals(id.Id) &&
                   EqualityComparer<TypeId[]>.Default.Equals(Subtypes, id.Subtypes);
        }

        public override int GetHashCode() {
            var hashCode = -1531856470;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeId[]>.Default.GetHashCode(Subtypes);
            return hashCode;
        }
        public override string ToString() {
            return $"{Id}";
        }
    }
}
