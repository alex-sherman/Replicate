using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Replicate.Web {
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public abstract class RPCMiddlewareAttribute : Attribute {
        public abstract Task Run(HttpContext context);
    }
    public class ReplicateRouteAttribute : Attribute {
        public string Route;
        public EnvironmentType Environments = EnvironmentType.All;
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FromDIAttribute : Attribute {
        public bool Optional = false;
    }
    /// <summary>
    /// NOTE: Configuration members must be __PROPERTIES__ to be filled in
    /// </summary>
    public class ConfigOptionsAttribute : Attribute { public string Section; }
}
