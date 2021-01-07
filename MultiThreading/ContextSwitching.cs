using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;



namespace MultiThreading
{
    public static class ContextSwitching
    {
        public static async Task DemonstrateThreadContextSwitching(bool MaintainOriginalThreadContext = true)
        {
            try
            {
                if (MaintainOriginalThreadContext)
                    Console.WriteLine($"\r\n\r\nDemonstrates how to maintain the current thread context-----------------------------------------------------");
                else
                    Console.WriteLine($"\r\n\r\nDemonstrates thread context swtich cause by await----------------------------------------------------------");

                // Using await will usually change your execution context to that of a thread pool thread and not the thread you started on.
                // If you make a blocking ".Wait()" or ".Result" call, then you will stay on the calling thread.

                // Create a task and supply a user delegate by using a lambda expression.
                Console.WriteLine($"Before task start, Calling thread Name '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} . IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");

                Task taskA = new Task(() =>
                {
                    SetThreadName("Task thread", Thread.CurrentThread);
                    Thread.Sleep(5000);
                    Console.WriteLine($"Inside task, Thread Name = '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} . IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");
                });

                // Start the task.
                taskA.Start();

                // Show that the current thread has not changed and we are alive
                Console.WriteLine($"Task started, Calling thread Name '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} . IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");

                // Wait for task to complete
                if (MaintainOriginalThreadContext)
                {
                    taskA.Wait();
                }
                else
                {
                    await taskA;   // Context switch might occur
                }
                Console.WriteLine($"Task has completed, Calling thread Name '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} .IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");



                Task<string> paul = SleepForAWhile("Paul");
                Task<string> tom = SleepForAWhile("Tom");
                Task<string> steve = SleepForAWhile("Steve");
                List<Task<string>> sleepers = new List<Task<string>>();
                sleepers.Add(paul);
                sleepers.Add(tom);
                sleepers.Add(steve);

                Console.WriteLine("I'm waiting for sleepers in main thread");

                string[] allMessages = null;
                if (MaintainOriginalThreadContext)
                {
                    allMessages = Task.WhenAll(sleepers).Result;
                }
                else
                {
                    allMessages = await Task.WhenAll(sleepers);
                }

                foreach (string Message in allMessages)
                {
                    Console.WriteLine(Message);
                }

                Console.WriteLine($"Sleepers now awake, Calling thread Name '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} .IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");

            }
            catch(Exception Err)
            {
                Console.WriteLine(Err.Message);
            }

        }

        static async Task<String> SleepForAWhile(string Name)
        {
            return await Task.Run(() => {
                SetThreadName($"Sleeper {Name}", Thread.CurrentThread);
                Console.WriteLine($"{Name} is sleeping in thread '{Thread.CurrentThread.Name}', id = {Thread.CurrentThread.ManagedThreadId} .IsThreadPoolThread={Thread.CurrentThread.IsThreadPoolThread}");
                Thread.Sleep(10000);
                return ($"{Name} finished sleeping");
            });
        }

        static void SetThreadName(string Name, Thread CurrentThread)
        {
            if (string.IsNullOrEmpty(CurrentThread.Name))
                CurrentThread.Name = Name;
        }
    }
}

