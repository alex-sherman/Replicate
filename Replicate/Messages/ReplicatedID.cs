﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Messages
{
    [ReplicateType]
    public struct ReplicatedId
    {
        [Replicate]
        public ushort Creator;
        [Replicate]
        public uint ObjectID;

        public override int GetHashCode()
        {
            return (23 * 31 + Creator.GetHashCode()) * 31 + ObjectID.GetHashCode();
        }
    }
}
