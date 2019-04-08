using Replicate.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public static class Util
    {
        public static bool IsSameGeneric(this Type compare, Type target)
        {
            return (compare.IsGenericTypeDefinition && compare == target) ||
                (compare.IsGenericType && compare.GetGenericTypeDefinition() == target);
        }
        public static async Task<object> Taskify(Type type, object obj)
        {
            if (obj is Task task)
            {
                await task;
                if (type == typeof(Task))
                    return None.Value;
                if (type.IsSameGeneric(typeof(Task<>)))
                    return (object)((dynamic)task).Result;
            }
            return obj;
        }

        public static Type GetTaskReturnType(this Type type)
        {
            if (type == typeof(Task) || type == typeof(void))
                return typeof(None);
            if (type.IsSameGeneric(typeof(Task<>)))
                return type.GetGenericArguments()[0];
            return type;
        }
        public static void Await(this Task task)
        {
            task.GetAwaiter().GetResult();
        }
        public static T Output<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        public static HandlerDelegate CreateHandler(MethodInfo method, Func<RPCRequest, object> target)
        {
            var contract = new RPCContract(method);
            object[] args = method.GetParameters().Select(p => p.HasDefaultValue ? null : p.DefaultValue).ToArray();

            return request =>
            {
                using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true))
                {
                    if (contract.RequestType != typeof(None))
                        args[0] = request.Request;
                    try
                    {
                        var result = method.Invoke(target(request), args);
                        // TODO: This could be done with Reflection.Emit I think?
                        return Taskify(method.ReturnType, result);
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw e.InnerException;
                    }
                }
            };
        }
    }
}
