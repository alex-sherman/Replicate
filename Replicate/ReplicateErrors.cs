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
        public ReplicateError() { }
    }
    public class ContractNotFoundError : ReplicateError
    {
        public ContractNotFoundError(string message) : base(message) { }
        public ContractNotFoundError() { }
    }
    public class SerializationError : ReplicateError
    {
        public SerializationError(string message) : base(message) { }
        public SerializationError() { }
    }
}