using Replicate.MetaTyping;
using Replicate.Messages;
using Replicate.MetaData;
using Replicate.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Replicate
{
    public static class ChannelExtensions
    {
        public static void Respond(this IRPCServer server, MethodInfo method, HandlerDelegate handler)
        {
            server.Respond(server.Model.MethodKey(method), handler);
        }
        public static void RespondRaw(this IRPCServer server, HandlerDelegate handler)
        {
            server.Respond(server.Model.MethodKey(handler.Method), handler);
        }
        public static void Respond<TRequest, TResponse>(this IRPCServer server, Func<TRequest, Task<TResponse>> handler)
        {
            server.Respond(server.Model.MethodKey(handler.Method), async (req) => { return await handler(TypeUtil.Cast<TRequest>(req.Request)); });
        }
        public static void Respond<TRequest>(this IRPCServer server, Action<TRequest> handler)
        {
            server.Respond(server.Model.MethodKey(handler.Method), (req) =>
            {
                handler(TypeUtil.Cast<TRequest>(req.Request));
                return Task.FromResult<object>(None.Value);
            });
        }

        public static void RegisterSingleton<T>(this IRPCServer channel, T implementation)
        {
            foreach (var method in channel.Model[typeof(T)].RPCMethods)
                channel.Respond(method, TypeUtil.CreateHandler(method, _ => implementation));
        }
        public static async Task<T> Request<T>(this IRPCChannel channel, Expression<Func<Task<T>>> call)
        {
            var methodCall = call.Body as MethodCallExpression;
            var arguments = methodCall.EvaluateArguments();
            return (T)(await channel.Request(methodCall.Method, arguments.Length > 0 ? arguments[0] : None.Value));
        }
        /// <summary>
        /// Avoid using this since there is no type checking on request/response
        /// </summary>
        public static Task<object> Request(this IRPCChannel channel, MethodInfo method, object request, ReliabilityMode reliability = ReliabilityMode.ReliableSequenced)
        {
            return channel.Request(new RPCRequest()
            {
                Endpoint = channel.Model.MethodKey(method),
                Contract = new RPCContract(method),
                Request = request
            }, reliability);
        }

        public static T CreateProxy<T>(this IRPCChannel channel, ReplicateId? target = null) where T : class
        {
            return ProxyImplement.HookUp<T>(new ReplicatedProxy(target, channel, typeof(T)));
        }
    }
}
