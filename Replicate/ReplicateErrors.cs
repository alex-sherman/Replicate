﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public class ReplicateError : Exception
    {
        public ReplicateError(string message) : base(message) { }
        public ReplicateError(string message, Exception exception) : base(message, exception) { }
        public ReplicateError() { }
    }
    public class ReplicatedReferenceError : ReplicateError
    {
        public ReplicatedReferenceError(string message) : base(message) { }
        public ReplicatedReferenceError(string message, Exception exception) : base(message, exception) { }
        public ReplicatedReferenceError() { }
    }
    public class ContractNotFoundError : ReplicateError
    {
        public ContractNotFoundError(string message) : base(message) { }
        public ContractNotFoundError(string message, Exception exception) : base(message, exception) { }
        public ContractNotFoundError() { }
    }
    public class SerializationError : ReplicateError
    {
        public SerializationError(string message) : base(message) { }
        public SerializationError(string message, Exception exception) : base(message, exception) { }
        public SerializationError() { }
    }
}
