using Replicate.MetaData;

namespace Replicate.Messages {
    [ReplicateType]
    public struct InitMessage {
        [Replicate]
        public ReplicateId id;
        [Replicate]
        public TypeId typeID;
    }
}
