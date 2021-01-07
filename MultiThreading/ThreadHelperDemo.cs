using System;
using System.Collections.Generic;
using System.Threading;


namespace MultiThreading
{
    public static class ThreadHelperDemo
    {
        public static void RunDemo()
        {

            Console.WriteLine("\r\n\r\nThreadHelper demo --------------------------------------------------------");

            // Now we will demonstrate how to execute threads using our ThreadHelper class.
            // We can pass in ANY "Action" delegate (no parameters and no response). We can use
            // any object method to matches the Action delegate signature. This also means we
            // can use a lambda expression with a matching signature.
            ThreadHelper.RunToCompletion(() =>
            {
                for (int i = 0; i < 10; ++i)
                {
                    Console.WriteLine($"Direct execution of lambda id={Thread.CurrentThread.ManagedThreadId} printing {i}");
                    Thread.Sleep(500);
                }
            });

            // Assign a lambda to an Action delegate
            Action lambdaAction = () => {
                for (int i = 0; i < 10; ++i)
                {
                    Console.WriteLine($"Lambda action id={Thread.CurrentThread.ManagedThreadId} printing {i}");
                    Thread.Sleep(500);
                }
            };

            // Uses extension method to executes the Action delegate and wait for the thread to finish
            lambdaAction.ExecuteAsThread();

            // Executes the same Action but does NOT wait for the thread to finish.
            // We will use the returned WaitHandle to wait for it to complete.
            WaitHandle actionWaiter = lambdaAction.Run();
            for (int z = 0; z < 100; ++z)
            {
                Console.WriteLine($"Main thread id={Thread.CurrentThread.ManagedThreadId} doing work {z}");
                Thread.Sleep(50);
            }
            Console.WriteLine($"Main thread id={Thread.CurrentThread.ManagedThreadId} finished working and waiting for Action delegate to complete");
            actionWaiter.WaitOne();


            Console.WriteLine("About to execute a Func<T,R> delegate thread that returns a value");

            // This version of RunToCompletion takes 2 parameters. The first is the labda expression
            // that will be executed. The second is the parameter that will be passed into the lamda expression.
            int Total = ThreadHelper.RunToCompletion<List<int>, int>((inputList) =>
            {
                int TotalSofar = 0;
                foreach (int i in inputList)
                {
                    Console.WriteLine($"Thread printing intput value {i}");
                    Thread.Sleep(500);
                    TotalSofar += i;
                }
                return (TotalSofar);
            }
            // This is the "inputList" parameter for the "RunToCompletion" lambda expression
            , new List<int> { 1, 5, 6, 7, 8 }
            );

            Console.WriteLine($"RunToCompletion value returned is {Total}");

            //----- Instead of using lambda expressions to execute Action or Func delegates, we can use 
            //      methods of an object as the delegate.
            //      We show 4 different ways to execute the Action "DoSomething" to completion
            TestClass t = new TestClass();
            ((Action)t.DoSomething).ExecuteAsThread();
            Console.WriteLine($"DoSomething finished 1");

            Action MyAction = t.DoSomething;
            MyAction.ExecuteAsThread();
            Console.WriteLine($"DoSomething finished 2");

            ThreadHelper.ExecuteAsThread(t.DoSomething);
            Console.WriteLine($"DoSomething finished 3");

            ThreadHelper.RunToCompletion(t.DoSomething);
            Console.WriteLine($"DoSomething finished 4");


            //  We show 4 different ways to execute the Func<T,R> "AddThese" to completion
            List<int> myList = new List<int> { 1, 5, 6, 7, 8 };
            int myTotal = ((Func<List<int>, int>)t.AddThese).ExecuteAsThread(myList);
            Console.WriteLine($"AddThese finished method 1 = {myTotal}");

            Func<List<int>, int> addTheseFunc = t.AddThese;
            myTotal = addTheseFunc.ExecuteAsThread(myList);
            Console.WriteLine($"AddThese finished method 2 = {myTotal}");

            myTotal = ThreadHelper.ExecuteAsThread(t.AddThese, myList);
            Console.WriteLine($"AddThese finished method 3 = {myTotal}");

            myTotal = ThreadHelper.RunToCompletion(t.AddThese, myList);
            Console.WriteLine($"AddThese finished method 4 = {myTotal}");


            // This time we'll run the code in a thread and then
            // do something else while we wait for the thread to do its thing.
            TestClass t2 = new TestClass();
            List<int> moreNumbers = new List<int> { 1, 5, 6, 7, 8, 3, 9, 15, 4 };
            Func<List<int>, int> adder = t2.AddTheseWithWait;
            ThreadResult<int> waiter = adder.Run(moreNumbers);

            for (int x = 0; x < 10; ++x)
            {
                Console.WriteLine($"Main thread doing some work {x}");
                Thread.Sleep(50);
            }

            // Will block until the "adder" thread completes and returns a result
            myTotal = waiter.Result;

            Console.WriteLine($"waiter returned {myTotal}");


            // Now we'll demonstrate running a lambda expression as a thread.
            // The main thread will continue to execute and then wait for the 
            // lambda to complete.
            var labdaWaiter = ThreadHelper.Run((TheList) => {
                int total = 0;
                foreach (int i in TheList)
                {
                    Thread.Sleep(100);
                    total -= i;
                    Console.WriteLine($"Lamda Thread id={Thread.CurrentThread.ManagedThreadId} total so far {total}");
                }
                return (total);
            }, moreNumbers);

            for (int x = 0; x < 50; ++x)
            {
                Console.WriteLine($"Main thread id={Thread.CurrentThread.ManagedThreadId} waiting {x}");
                Thread.Sleep(50);
            }
            Console.WriteLine($"Main thread {Thread.CurrentThread.ManagedThreadId}  Lambda waiter returned {labdaWaiter.Result}");


        }
    }
}
