﻿using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct InitMessage
    {
        public ReplicateId id;
        public TypeId typeID;
    }
}
