﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct ReplicateId
    {
        public ushort Creator;
        public uint ObjectID;

        public override int GetHashCode()
        {
            return (23 * 31 + Creator.GetHashCode()) * 31 + ObjectID.GetHashCode();
        }
    }
}
