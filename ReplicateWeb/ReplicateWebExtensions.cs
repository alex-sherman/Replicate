﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    [Flags]
    public enum EnvironmentType
    {
        None = 0,
        Development = 1,
        Staging = 1 << 1,
        Production = 1 << 2,
        All = Development | Staging | Production,
        NotProd = Development | Staging,
    }
    public static class ReplicateWebExtensions
    {
        public static EnvironmentType GetEnvironmentType(this IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) return EnvironmentType.Development;
            if (env.IsStaging()) return EnvironmentType.Staging;
            if (env.IsProduction()) return EnvironmentType.Production;
            return EnvironmentType.None;
        }
        public static (int StatusCode, ErrorData Error) FromException(Exception exception)
        {
            while (exception is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
                exception = aggregate.InnerExceptions[0];
            return (exception is HTTPError e ? e.Status : 500, new ErrorData(exception));
        }
        public static string GetRoute(MethodInfo endpoint)
        {
            var methodRoute = endpoint.GetCustomAttribute<ReplicateRouteAttribute>();
            var name = methodRoute?.Route ?? endpoint.Name.ToLower();
            var classRoute = endpoint.DeclaringType.GetCustomAttribute<ReplicateRouteAttribute>();
            if (classRoute?.Route != null)
                name = $"{classRoute.Route}/{name}";
            while (name.Any() && name.Last() == '/')
                name = name.Substring(0, name.Length - 1);
            return name;
        }
        public static ReplicateRouteAttribute GetRouteAttribute(MethodInfo endpoint)
        {
            var methodRoute = endpoint.GetCustomAttribute<ReplicateRouteAttribute>();
            if (methodRoute == null) methodRoute = new ReplicateRouteAttribute();
            methodRoute.Route = GetRoute(endpoint);
            return methodRoute;
        }
        public static void UseEndpoints(this IApplicationBuilder app, IWebHostEnvironment env, IReplicateSerializer serializer)
        {
            var model = serializer.Model;
            model.LoadTypes(typeof(ErrorData).Assembly);
            var serviceTypes = model.Types.Values.Where(t => t.Type.GetCustomAttribute<ReplicateRouteAttribute>() != null).ToList();
            var implTypes = serviceTypes
                .Select(st => st.Type.IsInterface
                    ? model.Types.Values.FirstOrDefault(mt => mt.Type.Implements(st.Type))
                    : st)
                .Where(t => t != null).ToList();
            var routes = implTypes.SelectMany(t => t.Methods)
                .Select(m => (Attribute: GetRouteAttribute(m), Key: model.MethodKey(m)))
                .Where(route => route.Attribute.Environments.HasFlag(env.GetEnvironmentType()));
            app.UseEndpoints(e =>
            {
                foreach (var route in routes)
                {
                    e.Map(RoutePatternFactory.Parse(route.Attribute.Route), async context =>
                    {
                        var method = model.GetMethod(route.Key);
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
                            Endpoint = route.Key,
                            Request = stream.Length == 0 ? null : serializer.Deserialize(contract.RequestType, stream)
                        });
                        ILogger logger = context.RequestServices.GetService<ILogger<ReplicateWebRPC>>();
                        logger?.LogDebug($"{route.Attribute.Route}({context.TraceIdentifier}) <= {Encoding.UTF8.GetString(stream.ToArray())}");
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 200;
                        var responseString = serializer.SerializeString(contract.ResponseType, result);
                        logger?.LogDebug($"({context.TraceIdentifier}) => {responseString}");
                        await context.Response.WriteAsync(responseString);
                    });
                }
            });
        }
        public static void UseErrorHandling(this IApplicationBuilder app, IReplicateSerializer serializer)
        {
            app.Use((context, next) => next().ContinueWith(t =>
            {
                if (t.Exception == null) return t;
                var e = t.Exception;
                context.RequestServices.GetService<ILogger<HTTPError>>()?.LogError(e, "Handler exception");
                var (StatusCode, Error) = FromException(e);
                context.Response.StatusCode = StatusCode;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(serializer.Serialize(Error).ReadAllString());
            }));
        }
    }
}
