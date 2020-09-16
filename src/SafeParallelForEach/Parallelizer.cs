namespace SafeParallelForEach
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Parallelizer
    {
        /// <summary>
        /// Runs the action over all the input items.
        /// There is no error handling - if the action throws an exception, it will blow and stop processing eventually.
        /// If desired you can put error handling in the action.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with 
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        public static async Task SafeParallel<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            var taskQueue = new Queue<Task>();

            // var tl = new List<Task>();
            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();
                var task = action(input);
                taskQueue.Enqueue(RunIt(task, sem));

                // something like while stack.peek.iscompleted yield return? No, that will pause it. But, I could at least await it... Though, if I do yield return but only when it's done..? I want a pipeline really 'cause this is all about buffering but with yield return it just comes down to how fast the consumer consumes it. So that's probably ok???
                while (taskQueue.Peek().IsCompleted)
                {
                    await taskQueue.Dequeue();
                }
            }

            await Task.WhenAll(taskQueue);
        }

        public static async IAsyncEnumerable<Result<TIn>> SafeParrallelWithResult<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            var taskQueue = new Queue<Task<Result<TIn>>>();

            
            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();
                
                taskQueue.Enqueue(RunIt(input, action, sem));

                // Return the tasks that have already compleed
                while (taskQueue.Peek().IsCompleted)
                {
                    // As far as I can fathom, there is no way this could throw an exception so not handling it
                    yield return await taskQueue.Dequeue();
                }
            }

            foreach (var task in taskQueue)
            {
                yield return await task;
            }
        }

        // public static async IAsyncEnumerable<Result<TIn>> SafeParrallelWithResult<TIn, TOut>(this IEnumerable<TIn> inputValue, Func<TIn, Task<TOut>> action, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        private static async Task<Result<TIn>> RunIt<TIn>(TIn input, Func<TIn, Task> action, SemaphoreSlim sem)
        {
            try
            {
                await action(input);
                return new Result<TIn>(input);
            }
            catch(Exception e)
            {
                return new Result<TIn>(input, e);
            }
            finally{
                sem.Release();
            }
            
        }

        private static async Task RunIt(Task task, SemaphoreSlim sem)
        {
            await task;
            sem.Release();
        }

        // Can I use IAsyncEnumerable to somehow stream the results back???
        // Action<Task> callback = null ??
        // exception handling
    }
}