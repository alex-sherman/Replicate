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
        public static void Respond(this IRPCChannel channel, HandlerDelegate handler)
        {
            channel.Respond(handler.Method, handler);
        }
        public static void Respond<TRequest, TResponse>(this IRPCChannel channel, Func<TRequest, Task<TResponse>> handler)
        {
            channel.Respond(handler.Method, async (req) => { return await handler(TypeUtil.Cast<TRequest>(req.Request)); });
        }
        public static void Respond<TRequest>(this IRPCChannel channel, Action<TRequest> handler)
        {
            channel.Respond(handler.Method, (req) => { handler(TypeUtil.Cast<TRequest>(req.Request)); return null; });
        }

        public static MethodInfo StaticMethod(Expression<Action> method)
        {
            return (method.Body as MethodCallExpression).Method;
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
            return channel.Request(method, new RPCRequest()
            {
                Contract = new RPCContract(method),
                Request = request
            }, reliability);
        }

        public static void RegisterSingleton<T>(this IRPCChannel channel, T implementation)
        {
            foreach (var method in ReplicationModel.Default.Add(typeof(T)).RPCMethods)
                channel.Respond(method, TypeUtil.CreateHandler(method, _ => implementation));
        }

        public static T CreateProxy<T>(this IRPCChannel channel, ReplicateId? target = null) where T : class
        {
            return ProxyImplement.HookUp<T>(new ReplicatedProxy(target, channel, typeof(T)));
        }
    }
}
