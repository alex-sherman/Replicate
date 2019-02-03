using Replicate.Interfaces;
using Replicate.Messages;
using Replicate.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            channel.Respond(handler.Method, async (req) => { return await handler((TRequest)req.Request); });
        }
        public static void Respond<TRequest>(this IRPCChannel channel, Action<TRequest> handler)
        {
            channel.Respond(handler.Method, (req) => { handler((TRequest)req.Request); return null; });
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
            foreach (var method in typeof(T).GetMethods())
                channel.Respond(method, (request) => Util.RPCInvoke(method, implementation, request.Request));
        }

        public static T CreateProxy<T>(this IRPCChannel channel, ReplicateId? target = null) where T : class
        {
            return ProxyImplement.HookUp<T>(new ReplicatedProxy(target, channel, typeof(T)));
        }
    }
}
