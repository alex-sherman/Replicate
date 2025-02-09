namespace Replicate.Messages {
    [ReplicateType]
    public struct ReplicateId {
        public ushort Creator;
        public uint ObjectID;

        public override int GetHashCode() {
            return (23 * 31 + Creator.GetHashCode()) * 31 + ObjectID.GetHashCode();
        }
    }
}
