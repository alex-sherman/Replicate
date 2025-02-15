using System.Collections.Generic;

namespace Replicate.MetaData {
    [ReplicateType]
    public struct MethodKey {
        [Replicate]
        public TypeId Type;
        [Replicate]
        public RepKey Method;

        public override bool Equals(object obj) {
            if (!(obj is MethodKey)) {
                return false;
            }

            var key = (MethodKey)obj;
            return EqualityComparer<TypeId>.Default.Equals(Type, key.Type) &&
                   EqualityComparer<RepKey>.Default.Equals(Method, key.Method);
        }

        public override int GetHashCode() {
            var hashCode = 314988997;
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeId>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<RepKey>.Default.GetHashCode(Method);
            return hashCode;
        }
        public override string ToString() {
            return $"{Type}.{Method}";
        }
    }
}
