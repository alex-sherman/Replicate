using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
    public abstract class CustomAuthAttribute : Attribute
    {
        public abstract void ThrowIfUnverified();
    }
    public class ReplicateRouteAttribute : Attribute
    {
        public string Route;
    }
    public class HTTPError : Exception
    {
        public int Status = 500;
        public HTTPError(string message, int status = 500) : base(message) { Status = status; }
    }
    public class WebRPCServer : RPCServer
    {
        public WebRPCServer(ReplicationModel model) : base(model)
        {
        }

        public MethodKey GetKey(string path)
        {
            return Methods.Select(m => (MethodKey?)m).FirstOrDefault(m => GetEndpoint(Model.GetMethod(m.Value)) == path)
                ?? throw new ContractNotFoundError();
        }

        public string GetEndpoint(MethodInfo endpoint)
        {
            var methodRoute = endpoint.GetCustomAttribute<ReplicateRouteAttribute>();
            var name = methodRoute?.Route ?? endpoint.Name.ToLower();
            var classRoute = endpoint.DeclaringType.GetCustomAttribute<ReplicateRouteAttribute>();
            if (classRoute != null)
                name = $"{classRoute.Route}/{name}";
            while (name.Any() && name.Last() == '/')
                name = name.Substring(0, name.Length - 1);
            return name;
        }
    }
    public class ReplicateController : Controller
    {
        private static AsyncLocal<ActionContext> context = new AsyncLocal<ActionContext>();
        public static ActionContext CurrentContext => context.Value;

        public readonly JSONSerializer Serializer;
        public readonly WebRPCServer Server;
        public static IServiceProvider Services { get; protected set; }
        readonly ILogger logger;
        public ReplicateController(IServiceProvider services)
        {
            Serializer = services.GetRequiredService<JSONSerializer>();
            Server = new WebRPCServer(Serializer.Model);
            logger = services.GetService<ILogger<ReplicateController>>();
            Services = services;
        }

        // GET api/values
        [HttpGet("{*path}")]
        [HttpPost("{*path}")]
        [HttpOptions("{*path}")]
        public virtual async Task<ActionResult> Post(string path)
        {
            context.Value = ControllerContext;
            try
            {
                logger?.LogInformation($"Beginning request to {path}");
                return await Handle(path);
            }
            catch (ContractNotFoundError)
            {
                return new NotFoundResult();
            }
            finally
            {
                context.Value = null;
                logger?.LogInformation($"Finished request to {path}");
            }
        }
        public virtual async Task<ActionResult> Handle(string path)
        {
            path = path ?? "";
            while (path.Any() && path.Last() == '/')
                path = path.Substring(0, path.Length - 1);

            try
            {
                var methodKey = Server.GetKey(path);
                var method = Server.Model.GetMethod(methodKey);
                method.GetCustomAttribute<CustomAuthAttribute>()?.ThrowIfUnverified();
                var contract = Server.Contract(methodKey);
                var result = await Server.Handle(new RPCRequest()
                {
                    Contract = contract,
                    Endpoint = methodKey,
                    Request = Serializer.Deserialize(contract.RequestType, Request.Body)
                });
                if (result is ActionResult actionResult)
                    return actionResult;
                return new ContentResult()
                {
                    Content = Serializer.SerializeString(contract.ResponseType, result),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch (ContractNotFoundError)
            {
                return new NotFoundResult();
            }
        }
    }
}
