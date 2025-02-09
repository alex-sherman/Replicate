using Replicate.MetaData;

namespace Replicate.Messages {
    [ReplicateType]
    public struct InitMessage {
        public ReplicateId id;
        public TypeId typeID;
    }
}
