using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaTyping
{
    public static class DynamicModule
    {
        public static ModuleBuilder Create()
        {
            AssemblyName assemblyName = new AssemblyName("ReplicateDynamicAssembly");
            AssemblyBuilder assemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            return assemBuilder.DefineDynamicModule("ReplicateDynamicModule");
        }
    }
}
