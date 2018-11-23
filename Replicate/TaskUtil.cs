using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
