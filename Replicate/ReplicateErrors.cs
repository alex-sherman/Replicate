using System;
using System.IO;

namespace Replicate {
    public class ReplicateError : Exception {
        public ReplicateError(string message) : base(message) { }
        public ReplicateError(string message, Exception exception) : base(message, exception) { }
        public ReplicateError() { }
    }
    public class ReplicatedReferenceError : ReplicateError {
        public ReplicatedReferenceError(string message) : base(message) { }
        public ReplicatedReferenceError(string message, Exception exception) : base(message, exception) { }
        public ReplicatedReferenceError() { }
    }
    public class ContractNotFoundError : ArgumentOutOfRangeException {
        public ContractNotFoundError(string message) : base(message) { }
        public ContractNotFoundError(string message, Exception exception) : base(message, exception) { }
        public ContractNotFoundError() { }
    }
    public class SerializationError : ReplicateError {
        public SerializationError(Stream stream) : base($"Error at position {stream.Position}") { }
        public SerializationError(string message, Stream stream) : base($"Error at position {stream.Position}: {message}") { }
        public SerializationError(string message, Exception exception) : base(message, exception) { }
        public SerializationError() { }
    }
}
