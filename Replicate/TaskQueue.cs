using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate
{
    public class TaskQueue<T>
    {
        private Queue<T> queue = new Queue<T>();
        private Queue<TaskCompletionSource<T>> sources = new Queue<TaskCompletionSource<T>>();

        public void Put(T item)
        {
            lock (queue)
            {
                if (sources.Any())
                    sources.Dequeue().SetResult(item);
                else
                    queue.Enqueue(item);
            }
        }
        public Task<T> Poll()
        {
            lock (queue)
            {
                if (queue.Any())
                    return Task.FromResult(queue.Dequeue());
                var source = new TaskCompletionSource<T>();
                sources.Enqueue(source);
                return source.Task;
            }
        }
    }
}
