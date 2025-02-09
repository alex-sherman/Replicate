using Replicate.Messages;
using Replicate.MetaData;

namespace Replicate.RPC {
    public struct RPCRequest {
        public MethodKey Endpoint;
        public RPCContract Contract;
        public object Request;
        /// <summary>
        /// Target may be null if not in an instance RPC
        /// </summary>
        public ReplicateId? Target;
    }
}
