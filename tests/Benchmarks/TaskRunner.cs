namespace Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using SafeParallel;

    [MemoryDiagnoser]
    public class TaskRunner
    {
        private static IList<int> inputdata = Enumerable.Range(1, 100000).ToList();

        private static Func<int, Task> justRunItAction = async (int i) => await Task.Delay(10);

        private static Func<int, Task<int>> runWithReturnAction = async (int i) =>
        {
            await Task.Delay(10);
            return i * 2;
        };

        [Benchmark]
        public async Task JustRunIt()
        {
            await inputdata.SafeParallelAsync(justRunItAction, 1000);
        }

        [Benchmark]
        public async Task RunWithReturnedInput()
        {
            await foreach (var result in inputdata.SafeParallelAsyncWithResult(justRunItAction, 1000))
            {
            }
        }

        [Benchmark]
        public async Task RunWithReturnedOutput()
        {
            await foreach (var result in inputdata.SafeParallelAsyncWithResult(runWithReturnAction, 1000))
            {
            }
        }
    }
}