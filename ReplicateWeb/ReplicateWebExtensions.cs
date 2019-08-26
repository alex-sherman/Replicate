using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Replicate;
using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Web
{
    public static class ReplicateWebExtensions
    {
        public static (int StatusCode, ErrorData Error) FromException(Exception exception)
            => (exception is HTTPError e ? e.Status : 500, new ErrorData(exception));
        public static string GetEndpoint(MethodInfo endpoint)
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
        public static void AddReplicate(this IServiceCollection services, IReplicateSerializer serializer)
        {
            var model = serializer.Model;
            model.LoadTypes(typeof(ErrorData).Assembly);
            var serviceTypes = model.Types.Values.Where(t => t.Type.GetCustomAttribute<ReplicateRouteAttribute>() != null).ToList();
            var implTypes = serviceTypes
                .Select(st => st.Type.IsInterface
                    ? model.Types.Values.FirstOrDefault(mt => mt.Type.Implements(st.Type))
                    : st)
                .Where(t => t != null).ToList();
            var routes = implTypes.SelectMany(t => t.RPCMethods).Select(m =>
            {
                var route = GetEndpoint(m);
                return (Route: route, Key: model.MethodKey(m));
            });
            var endpoints = new DefaultEndpointDataSource(routes.Select(r =>
                new RouteEndpoint(
                    async context =>
                    {
                        var method = model.GetMethod(r.Key);
                        var implementation = ActivatorUtilities.CreateInstance(context.RequestServices, method.DeclaringType);
                        var handler = TypeUtil.CreateHandler(method, _ => implementation);

                        var stream = new MemoryStream();
                        await context.Request.Body.CopyToAsync(stream);
                        stream.Position = 0;
                        await (method.GetCustomAttribute<RPCMiddlewareAttribute>()?.Run(context) ?? Task.FromResult(true));
                        var contract = new RPCContract(method);
                        var result = await handler(new RPCRequest()
                        {
                            Contract = contract,
                            Endpoint = r.Key,
                            Request = stream.Length == 0 ? null : serializer.Deserialize(contract.RequestType, stream)
                        });
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(serializer.SerializeString(contract.ResponseType, result));
                    },
                    RoutePatternFactory.Parse(r.Route),
                    0,
                    EndpointMetadataCollection.Empty,
                    r.Key.Type.Id.Name + ":" + r.Key.Method.Name
                    )
                ));
            services.AddRouting();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<EndpointDataSource>(endpoints));
        }
        public static void UseErrorHandling(this IApplicationBuilder app, IReplicateSerializer serializer)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception e)
                {
                    //logger?.LogError(e, "Handler exception");
                    var (StatusCode, Error) = FromException(e);
                    context.Response.StatusCode = StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(serializer.Serialize(Error).ReadAllString());
                }
            });
        }
    }
}
