using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Replicate;
using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;

namespace Replicate.Web
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public abstract class RPCMiddlewareAttribute : Attribute
    {
        public abstract Task Run(HttpContext context);
    }
    public class ReplicateRouteAttribute : Attribute
    {
        public string Route;
        public EnvironmentType Environments = EnvironmentType.All;
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FromDIAttribute : Attribute { }
    /// <summary>
    /// NOTE: Configuration members must be __PROPERTIES__ to be filled in
    /// </summary>
    public class ConfigOptionsAttribute : Attribute { public string Section; }
}
