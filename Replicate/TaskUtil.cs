using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public static class TaskUtil
    {
        public static async Task<object> Taskify(Type type, object obj)
        {
            if (obj is Task task)
            {
                await task;
                if (type == typeof(Task))
                    return None.Value;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                    return (object)((dynamic)task).Result;
            }
            return obj;
        }
        public static void Await(this Task task)
        {
            task.GetAwaiter().GetResult();
        }
        public static T Output<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        public static Task<object> RPCInvoke(MethodInfo method, object target, object request)
        {
            using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true))
            {
                object[] args = new object[] { };
                if (method.GetParameters().Length == 1)
                    args = new[] { request };
                var result = method.Invoke(target, args);
                // TODO: This could be done with Reflection.Emit I think?
                return TaskUtil.Taskify(method.ReturnType, result);
            }
        }
    }
}
