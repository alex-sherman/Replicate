using Replicate.MetaData;
using Replicate.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public static class TypeUtil
    {
        [Obsolete]
        public static T CopyFrom<T, U>(T target, U newFields, string[] whiteList = null, string[] blackList = null) where T : class
        {
            CopyToRaw(newFields, typeof(U), target, typeof(T), whiteList, blackList);
            return target;
        }
        public static T CopyTo<S, T>(S source, T target, string[] whiteList = null, string[] blackList = null) where T : class
        {
            CopyToRaw(source, typeof(S), target, typeof(T), whiteList, blackList);
            return target;
        }
        public static object CopyToRaw(object source, object target, string[] whiteList = null, string[] blackList = null)
        {
            return CopyToRaw(source, source.GetType(), target, target.GetType(), whiteList, blackList);
        }
        public static object CopyToRaw(object source, Type sourceType, object target, Type targetType, string[] whiteList = null, string[] blackList = null)
        {
            if (source == null) return target;
            var taTarget = ReplicationModel.Default.GetTypeAccessor(targetType);
            var taSource = taTarget;
            if (targetType != sourceType)
                taSource = ReplicationModel.Default.GetTypeAccessor(sourceType);
            if (target == null) target = taTarget.Construct();
            IEnumerable<MemberAccessor> members = taTarget.MemberAccessors;
            if (whiteList != null && whiteList.Any())
                members = members.Where(mem => whiteList.Contains(mem.Info.Name));
            if (blackList != null && blackList.Any())
                members = members.Where(mem => !blackList.Contains(mem.Info.Name));
            var memberTuples = members
                .Where(m => taSource.Members.ContainsKey(m.Info.Name))
                .Select(tMember => new { tMember, uMember = taSource.Members[tMember.Info.Name] })
                .Where(tuple => tuple.tMember.Type.IsAssignableFrom(tuple.uMember.Type)
                || (tuple.uMember.Type.IsSameGeneric(typeof(Nullable<>)) && tuple.tMember.Type == tuple.uMember.Type.GetGenericArguments()[0]));
            foreach (var tuple in memberTuples)
            {
                var newValue = tuple.uMember.GetValue(source);
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
        public static T Cast<T>(object obj)
        {
            return (T)(obj ?? default(T));
        }
        public static void Await(this Task task)
        {
            task.GetAwaiter().GetResult();
        }
        public static T Output<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type.IsEnum;
        }

        public static bool IsStruct(Type type)
        {
            return type.IsValueType && !IsPrimitive(type);
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
