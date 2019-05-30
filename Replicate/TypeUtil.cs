using Replicate.MetaData;
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
    public static class TypeUtil
    {
        public static T CopyFrom<T, U>(T target, U newFields, string[] whiteList = null, string[] blackList = null) where T : class
        {
            var taT = ReplicationModel.Default.GetTypeAccessor(typeof(T));
            var taU = taT;
            if (typeof(T) != typeof(U))
                taU = ReplicationModel.Default.GetTypeAccessor(typeof(U));
            IEnumerable<MemberAccessor> members = taT.MemberAccessors;
            if (whiteList != null && whiteList.Any())
                members = members.Where(mem => whiteList.Contains(mem.Info.Name));
            if (blackList != null && blackList.Any())
                members = members.Where(mem => !blackList.Contains(mem.Info.Name));
            var memberTuples = members
                .Where(m => taU.Members.ContainsKey(m.Info.Name))
                .Select(tMember => new { tMember, uMember = taU.Members[tMember.Info.Name] })
                .Where(tuple => tuple.tMember.Type == tuple.uMember.Type
                || (tuple.uMember.Type.IsSameGeneric(typeof(Nullable<>)) && tuple.tMember.Type == tuple.uMember.Type.GetGenericArguments()[0]));
            foreach (var tuple in memberTuples)
            {
                var newValue = tuple.uMember.GetValue(newFields);
                if (newValue == null) continue;
                tuple.tMember.SetValue(target, newValue);
            }
            return target;
        }
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
            object[] args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();

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
