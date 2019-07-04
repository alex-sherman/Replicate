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
        private static ModuleBuilder _module;
        public static ModuleBuilder Single => _module ?? (_module = Create());
        public static ModuleBuilder Create()
        {
            AssemblyName assemblyName = new AssemblyName("ReplicateDynamicAssembly");
            AssemblyBuilder assemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            return assemBuilder.DefineDynamicModule("ReplicateDynamicModule");
        }

    }
}
