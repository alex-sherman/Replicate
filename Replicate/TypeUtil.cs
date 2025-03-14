﻿using Replicate.MetaData;
using Replicate.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Replicate {
    public static class TypeUtil {
        // TODO: This might be a bad idea? Could theoretically remove this if all the copy methods allowed providing a ReplicationModel
        // The downside to having it is there might be cases where a user expects the TypeData to be used from ReplicationModel.Default
        private static ReplicationModel model;
        public static ReplicationModel Model => model ?? (model = new ReplicationModel() { AddOnLookup = true });
        [Obsolete]
        public static T CopyFrom<T, U>(T target, U newFields, string[] whiteList = null, string[] blackList = null) {
            return (T)CopyToRaw(newFields, typeof(U), target, typeof(T), whiteList, blackList);
        }
        public static T CopyTo<S, T>(S source, T target, string[] whiteList = null, string[] blackList = null) {
            return (T)CopyToRaw(source, typeof(S), target, typeof(T), whiteList, blackList);
        }
        public static object CopyToRaw(object source, object target, string[] whiteList = null, string[] blackList = null) {
            return CopyToRaw(source, source.GetType(), target, target.GetType(), whiteList, blackList);
        }
        public static object CopyToRaw(object source, Type sourceType, object target, Type targetType, string[] whiteList = null, string[] blackList = null) {
            var taTarget = Model.GetTypeAccessor(targetType);
            var taSource = taTarget;
            if (targetType != sourceType)
                taSource = Model.GetTypeAccessor(sourceType);
            return CopyToRaw(source, taSource, target, taTarget, whiteList, blackList);
        }
        public static object CopyToRaw(object source, TypeAccessor taSource, object target, TypeAccessor taTarget, string[] whiteList = null, string[] blackList = null) {
            if (source == null) return target;
            if (target == null) target = taTarget.Construct();
            IEnumerable<MemberAccessor> members = taTarget.Members.Values;
            if (whiteList != null && whiteList.Any())
                members = members.Where(mem => whiteList.Contains(mem.Info.Name));
            if (blackList != null && blackList.Any())
                members = members.Where(mem => !blackList.Contains(mem.Info.Name));
            var memberTuples = members
                .Where(m => taSource.Members.ContainsKey(m.Info.Name))
                .Select(tMember => new { tMember, uMember = taSource.Members[tMember.Info.Name] })
                .Where(tuple => tuple.tMember.Type.IsAssignableFrom(tuple.uMember.Type.DeNullable()));
            foreach (var tuple in memberTuples) {
                var newValue = tuple.uMember.GetValue(source);
                if (newValue == null) continue;
                tuple.tMember.SetValue(target, newValue);
            }
            return target;
        }
        public static bool Implements(this Type type, Type interfaceType) {
            return interfaceType == type || type.GetInterfaces().Any(t =>
                t == interfaceType || (interfaceType.IsGenericTypeDefinition && t.IsSameGeneric(interfaceType)));
        }
        /// <summary>
        /// Checks whether the types are the same, or have the same generic type definition,
        /// ignoring generic type arguments
        /// </summary>
        public static bool IsSameGeneric(this Type compare, Type target) {
            return compare == target ||
                (compare.IsGenericType && target.IsGenericType && compare.GetGenericTypeDefinition() == target.GetGenericTypeDefinition());
        }
        public static Type DeNullable(this Type type) {
            if (type.IsSameGeneric(typeof(Nullable<>))) return type.GetGenericArguments()[0];
            return type;
        }
        public static async Task<T> Taskify<T>(Type type, object obj) {
            if (obj is Task task) {
                await task;
                if (type == typeof(Task))
                    return default;
                return (T)type.GetProperty("Result").GetValue(obj);
                // For some reason this seems to have worst first-call performance, lasting about 400ms
                // return (object)((dynamic)task).Result;
            }
            return (T)obj;
        }

        public static Type GetTaskReturnType(this Type type) {
            if (type == typeof(Task) || type == typeof(void))
                return typeof(None);
            if (type.IsSameGeneric(typeof(Task<>)))
                return type.GetGenericArguments()[0];
            return type;
        }
        public static T Cast<T>(object obj) {
            return (T)(obj ?? default(T));
        }
        private static MethodInfo castTaskMethod = GetMethod(() => CastTask<object, object>(null)).GetGenericMethodDefinition();
        public static Task<T> CastTask<T, U>(Task<U> task) => task.ContinueWith(t => (T)(object)t.Result);
        public static T CastObjectTask<T>(Task<object> task) {
            var type = typeof(T);
            if (type.IsAssignableFrom(typeof(Task<object>))) return (T)(object)task;
            if (type.IsSameGeneric(typeof(Task<>))) {
                var taskTArg = type.GetGenericArguments()[0];
                var cast = castTaskMethod.MakeGenericMethod(taskTArg, typeof(object));
                return (T)cast.Invoke(null, new object[] { task });
            }
            return (T)task.GetAwaiter().GetResult();
        }
        private static MethodInfo GetMethod(LambdaExpression expression)
            => expression.Body is MethodCallExpression method ? method.Method
            : throw new ArgumentException("Expression is not a method", "expression");
        public static MethodInfo GetMethod(Expression<Action> expression) => GetMethod((LambdaExpression)expression);
        public static MethodInfo GetMethod<T>(Expression<Action<T>> expression) => GetMethod((LambdaExpression)expression);
        public static void Await(this Task task) {
            task.GetAwaiter().GetResult();
        }
        public static T Output<T>(this Task<T> task) {
            return task.GetAwaiter().GetResult();
        }

        public static bool IsPrimitive(Type type) {
            return type.IsPrimitive || type == typeof(string) || type.IsEnum;
        }

        public static bool IsStruct(Type type) {
            return type.IsValueType && !IsPrimitive(type);
        }

        public static HandlerDelegate CreateHandler(MethodInfo method, Func<RPCRequest, object> target) {
            var contract = new RPCContract(method);
            object[] args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();

            return request => {
                using (ReplicateContext.UpdateContext(r => r.Value._isInRPC = true)) {
                    if (contract.RequestType != typeof(None))
                        args[0] = request.Request;
                    try {
                        var result = method.Invoke(target(request), args);
                        // TODO: This could be done with Reflection.Emit I think?
                        return Taskify<object>(method.ReturnType, result);
                    } catch (TargetInvocationException e) {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw e.InnerException;
                    }
                }
            };
        }
    }
}
