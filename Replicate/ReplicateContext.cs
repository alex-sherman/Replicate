using System;
using System.Threading;

namespace Replicate {
    public struct ReplicateContext {
        class ContextTracker : IDisposable {
            ReplicateContext Context;
            public ContextTracker() => Context = Current;
            public void Dispose() => current.Value = Context;
        }
        public uint Client { get; internal set; }
        public ReplicationManager Manager { get; internal set; }
        internal bool _isInRPC;
        public static bool IsInRPC => Current._isInRPC;
        private static AsyncLocal<ReplicateContext> current = new AsyncLocal<ReplicateContext>();
        public static ReplicateContext Current => current.Value;
        internal static IDisposable UpdateContext(Action<Ref<ReplicateContext>> update) {
            var output = new ContextTracker();
            Ref<ReplicateContext> reference = Current;
            update(reference);
            current.Value = reference;
            return output;
        }
        internal static IDisposable UsingContext(ReplicateContext context) {
            var output = new ContextTracker();
            current.Value = context;
            return output;
        }
    }
}
