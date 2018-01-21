﻿using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Replicate
{
    public class ReplicateContext
    {
        public uint Client { get; internal set; }
        public ReplicationManager Manager { get; internal set; }
        public ReplicationModel Model { get; internal set; }
        internal bool _isInRPC;
        public static bool IsInRPC { get { return Current?._isInRPC ?? false; } }
        public static ReplicateContext Current { get; internal set; }
        // TODO: This is complaining about ReplicationManager not being Serializable.
        // Passing a deep copy of it around in the context will not work, so figure out how to
        // pass it around by reference or disallow async. (Consider using AsyncLocal<T> but that
        // probably uses the same LogicalGet/SetData.
        //{
        //    get { return (ReplicateContext)CallContext.LogicalGetData("Replicate.Context"); }
        //    internal set { CallContext.LogicalSetData("Replicate.Context", value); }
        //}
        internal static void Clear()
        {
            Current = null;
            //CallContext.FreeNamedDataSlot("Replicate.Context");
        }
    }
}
