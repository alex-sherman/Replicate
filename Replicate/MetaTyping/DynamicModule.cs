using System.Reflection;
using System.Reflection.Emit;

namespace Replicate.MetaTyping {
    public static class DynamicModule {
        public static ModuleBuilder Create() {
            try {

                AssemblyName assemblyName = new AssemblyName("ReplicateDynamicAssembly");
                AssemblyBuilder assemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                return assemBuilder.DefineDynamicModule("ReplicateDynamicModule");
            } catch {
                return null;
            }
        }
    }
}
