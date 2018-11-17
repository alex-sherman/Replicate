using Replicate.Messages;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Replicate
{
    public struct ReplicateContext
    {
        class ContextTracker : IDisposable
        {
            ReplicateContext Context;
            public ContextTracker() => Context = Current;
            public void Dispose() => current.Value = Context;
        }
        public uint Client { get; internal set; }
        public ReplicationManager Manager { get; internal set; }
        public Serializer Serializer { get; internal set; }
        internal bool _isInRPC;
        public static bool IsInRPC => Current._isInRPC;
        private static AsyncLocal<ReplicateContext> current = new AsyncLocal<ReplicateContext>();
        public static ReplicateContext Current => current.Value;
        internal static IDisposable UpdateContext(Action<Ref<ReplicateContext>> update)
        {
            var output = new ContextTracker();
            Ref<ReplicateContext> reference = Current;
            update(reference);
            current.Value = reference;
            return output;
        }
        internal static IDisposable UsingContext(ReplicateContext context)
        {
            var output = new ContextTracker();
            current.Value = context;
            return output;
        }
    }
}
