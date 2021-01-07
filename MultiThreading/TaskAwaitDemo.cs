using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreading
{
    public static class TaskAwaitDemo
    {
        public static async Task RunDemo()
        {
            await ContextSwitching.DemonstrateThreadContextSwitching();
            await ContextSwitching.DemonstrateThreadContextSwitching(false);

            // Shows you exactly what happens with async/await. The thread that calls "await"
            // blocks that thread until the the operation being awaited completes. 
            // Control is returned to the thread that calls the "async" method.
            // When the await completes, the code in the "async" method is continued.
            // "WaitFor" is an "async" method that contains a call to "await".
            // Therefore the "WaitFor" method gets started on a threadpool thread
            // and then blocks when it reaches "await". At that instant in time
            // this method "RunDemo"  will continue running. Once this RunDemo  reaches
            // the "await waitForTask", the RunDemo  is blocked until the Task
            // completes.
            Console.WriteLine("\r\n\r\n-----------------------------------------------------------------------");
            Task<string> waitForTask = WaitFor(10000);
            for (int x = 0; x < 5; ++x)
            {
                Console.WriteLine($"RunDemo  ( id = {Thread.CurrentThread.ManagedThreadId}) doing stuff while waitForTask runs {x}");
                Thread.Sleep(1000);
            }

            Console.WriteLine($"RunDemo  ( id = {Thread.CurrentThread.ManagedThreadId}) blocked awaiting Task to complete");
            string Msg = await waitForTask;
            Console.WriteLine($"RunDemo  ( id = {Thread.CurrentThread.ManagedThreadId}) WaitFor returned Msg = [{Msg}]. ");

            // Demonstrate same principle using an async lambda
            Func<string, Task> greet = async (name) =>
            {
                Console.WriteLine($"Hello from {name}! id={Thread.CurrentThread.ManagedThreadId}");
                await Task.Delay(5000);
                Console.WriteLine($"Async lambda id={Thread.CurrentThread.ManagedThreadId} completed");
            };

            Console.WriteLine($"RunDemo  id={Thread.CurrentThread.ManagedThreadId} waiting for async lambda");
            await greet("Async Lambda");

            // Demonstrates a task "continuation"
            Task continuationTask = Task.Run(()=>WaitFor(10000)).ContinueWith((taskToContinue) => {
                Console.WriteLine($"ContinueWith (id = {Thread.CurrentThread.ManagedThreadId}) has result = [{taskToContinue.Result}]");
            });

            for (int x = 0; x < 5; ++x)
            {
                Console.WriteLine($"RunDemo (id = {Thread.CurrentThread.ManagedThreadId}) doing stuff while continuation runs {x}");
                Thread.Sleep(1000);
            }

            // Wait for continuation to complete before we exit
            Console.WriteLine($"RunDemo (id = {Thread.CurrentThread.ManagedThreadId}) waiting for continuation task to complete");
            await continuationTask;
            Console.WriteLine($"RunDemo (id = {Thread.CurrentThread.ManagedThreadId}) continuation complete");
        }


        public static async Task<String> WaitFor(int Milliseconds)
        {
            int StartingThreadId = Thread.CurrentThread.ManagedThreadId;

            Console.WriteLine($"WaitFor starting on thread id = {Thread.CurrentThread.ManagedThreadId}");

            // This thread will block here and return control to the calling thread
            await Task.Delay(Milliseconds);

            Console.WriteLine($"Waitfor continuing on thread id = {Thread.CurrentThread.ManagedThreadId}");
            return $"WaitFor thread id = {Thread.CurrentThread.ManagedThreadId} finished";
        }
    }
}
