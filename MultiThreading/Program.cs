using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MultiThreading
{
    class Program
    {
        static async Task Main(string[] args)
        {
            /* 
                All thread-related classes and be found in the "System.Threading" namespace.

                A thread is defined as a path of execution within an application.
                On a single core machine, threading gives the "illusion" that multiple things are happening
                at the same time, when in fact the processor is simply context switching between threads,
                giving each one a small slice of time.  

                While many .NET Core applications only have a single thread, an application
                can create any number of secondary threads to make applications more responsive.
                On multi-core machines, threads can actually execute in parallel. Although 
                the developer has the ability to influence which processor a thread will run on
                and the priority with which it executes, altering these settings 
                (Thread affinity, ideal processor, priority) should generally be avoided as they
                can seriously impact the operating system's thread scheduler and can negatively
                impact the performance of your entire system.

                While threads give us the ability to perform multiple activities at the same time
                (or at least give that illusion), it creates a new problem, that of "concurrency"
                and the solution being "synchronization". When two or more threads operate on a single data item
                concurrently, one thread has the potential to inadvertently modify that data 
                item into a state that the other thread is not expecting. 
            
                To illustrate the problem, imagine a car driving down a road going east to west.
                With no other cars on the road, that car can go as fast as it wants and switch
                lanes without any issues. Now image that there is another road with a car traveling
                north to south. At some point in time these two roads intersect. Because there
                is only one car traveling in each direction, the probability that these two 
                cars will crash in the intersection is very slim. Now, let's increase the volume
                of cars traveling in each direction so that one car crosses the intersection 
                every fraction of a second in each direction. The probability that any two cars will collide
                in the intersection now becomes very high. This is a problem! The solution? Synchronization!
                In order to prevent any cars from colliding, we must "synchronize" access to the 
                intersection (the common area that any two cars can occupy at the same time).
                The obvious solution is to install traffic lights. The traffic lights are a 
                synchronization technique. By following the traffic light rules, each car 
                knows when it can and can't enter the intersection. Their movement has been
                synchronized by the traffic lights to prevent collision.

                Think of a thread as a road traveling in a certain direction and somewhere
                along its path it alters a data object "X". Now if you have multiple threads
                all trying to alter the same data object "X" at some point in their path
                you're bound to have a collision because data object "X" is the intersection.
                In order to prevent these collisions, multiple thread synchronization techniques
                have been created. We will illustrate some of these thread synchronization 
                techniques in this example.

                Ever since the first version of .Net, developers were able
                to create threads. However, the mechanism for doing this was rather cumbersome.
                You could use the "ThreadStart" or "ParameterizedThreadStart" delegate.  
                "ThreadStart" is used to start a thread that takes no parameters and returns nothing. 
                This somewhat limiting as many times the thread requires some kind of input
                parameter in order to know how to operate. To that end "ParameterizedThreadStart"
                takes a single "object" parameter and returns nothing.

                This next example simulates the problem of multiple cars entering
                an intersection at the same time. Each car's code runs in a separate thread. 
                In order to synchronize access to the intersection to prevent a crash
                an AutoResetEvent is used:

                AutoResetEvent maintains a boolean variable in memory. If the variable is false (not signaled)
                then it blocks the thread, when its true (signaled) it unblocks the thread. When we instantiate an
                AutoResetEvent object, we pass the default value of boolean value in the constructor. 
            
                Here's an example of how to create an AutoResetEvent:
                    AutoResetEvent autoResetEvent = new AutoResetEvent(true);

                "WaitOne" is a method of AutoResetEvent that will block the thread until the state
                is signaled (true), or for a specified period of time. 

                    e.g. Wait forever until signaled
                         autoResetEvent.WaitOne();

                    e.g. Wait for 2 seconds OR until signaled
                         autoResetEvent.WaitOne(TimeSpan.FromSeconds(2));

                "Set" is a method of AutoResetEvent that sends a signal to the waiting thread to proceed its work. 
                If multiple threads are blocked, only the first waiting thread is unblocked. The AutoResetEvent
                state is automatically reset to not signaled (hence auto reset event). Any other blocked threads
                will continue to be blocked.                    

                    e.g.  autoResetEvent.Set();

                By contrast, a "ManualResetEvent" blocks when the state is not signaled, but ALL blocked
                threads unblock when the ManualResetEvent is signaled. The ManualResetEvent remains
                in the signaled state until the "Reset" method is called to once again put the ManualResetEvent
                into a non-signaled state. We will use the ManualResetEvent to block all cars at the start
                of the race. Once all cars have initialized themselves and are ready to go, we unblock
                all cars simultaneously by calling the "Set" method.

                    e.g.  Create ManualResetEvent in the non-signaled state. Any thread calling 
                    StartAllCars.WaitOne() will block until StartAllCars.Set() is called.
                        ManualResetEvent StartAllCars = new ManualResetEvent(false);

                    e.g Block thread till "Set" is called
                        StartAllCars.WaitOne();

                    e.g. Unblock ALL threads that called StartAllCars.WaitOne()
                        StartAllCars.Set();

                Another synchronization mechanism we will demonstrate is the "Monitor".
                To ensure that only 1 thread enters a block of code at any time, 
                you "Enter" the monitor and when you're finished your task, you "Exit"
                the monitor. We use this mechanism to ensure that only 1 car can be
                added to a shared collection at any time. 

                    e.g 
                        private readonly object _lockobj = new Object();
                        Monitor.Enter(_lockobj);

                            .. Do some work here. Only 1 thread allowed in.

                        Monitor.Exit(_lockobj);

                    A C# simplification of the "Monitor" is the use of the "lock"
                    keyword. The compiler converts the call into the appropriate
                    monitor calls under the covers.

                        private readonly object _lockobj = new Object();
                        lock(_lockobj)
                        {
                            .. Do some work here. Only 1 thread allowed in.
                        }

                A more modern approach to threading (async/await). When you use the new async
                and await keywords, the compiler will generate a good deal of threading code on your behalf, 
                using numerous members of the System.Threading and System.Threading.Tasks namespaces.

                The async keyword of C# is used to qualify that a method, lambda expression, or anonymous method should
                be called in an asynchronous manner automatically. Simply by marking the method with the
                "async" modifier, a new thread of execution will be created to handle the task.
                The calling thread uses the "await" keyword to block until the task is complete.

                For I/O-bound code, you await an operation that returns a Task or Task<T> inside of an async method.
                For CPU-bound code, you await an operation that is started on a background thread with the Task.Run method.

                When you pass an Action delegate to Task.Run:

                        Task.Run(DoSomething);

                that’s exactly the same as:

                        Task.Factory.StartNew(DoSomething, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                Task.Run provides multiple overloads to support:

                    Synchronous vs asynchronous delegate
                    Task vs Task<TResult>
                    Cancelable vs non-cancelable
            */

            RaceCars();

            ThreadHelperDemo.RunDemo();

            Thread.CurrentThread.Name = "Main";
            await TaskAwaitDemo.RunDemo();

            Console.ReadKey();

        }

        public static void RaceCars()
        {
            // This example let's cars enter an intersection without stopping to check
            // if the intersection is clear. Many cars will crash!
            Console.WriteLine("------------------------------- Car CRASHES demo ----------------------------------");
            CarFactory.CreateFleet();
            CarFactory.DriveAllCars();
            CarFactory.WaitForAllCars();        // Waits for all cars to complete their trip


            // This example forces cars to stop before intersection and check
            // to make sure it is clear before entring. 
            Console.WriteLine("\r\n\r\n------------------------------- No crashes demo -----------------------------------");
            CarFactory.CreateFleet(20, true);
            CarFactory.DriveAllCars();
            CarFactory.WaitForAllCars();        // Waits for all cars to complete their trip
            IEnumerable<Car> winners = CarFactory.GetRaceWinners();
            foreach (Car c in winners)
            {
                Console.WriteLine($"{c.CarId} finished in position {c.FinishPosition}, with {c.NumberOfPitStopMade} pit stops");
            }

            // This example forces cars to stop before intersection and check
            // to make sure it is clear before entring. AND it will use ThreadPool threads instead of creating new ones. 
            Console.WriteLine("\r\n\r\n-------------------------Using ThreadPool threads ---------------------------------------------------------");
            CarFactory.CreateFleet(20, true);
            CarFactory.DriveAllCars(Car.ThreadingModel.ThreadPoolThreads);
            CarFactory.WaitForAllCars();        // Waits for all cars to complete their trip
            winners = CarFactory.GetRaceWinners();
            foreach (Car c in winners)
            {
                Console.WriteLine($"{c.CarId} finished in position {c.FinishPosition}, with {c.NumberOfPitStopMade} pit stops");
            }

            // This example forces cars to stop before intersection and check
            // to make sure it is clear before entring. AND it will use Task threads instead of creating new ones. 
            Console.WriteLine("\r\n\r\n-------------------------Using Task threads ---------------------------------------------------------");
            CarFactory.CreateFleet(20, true);
            CarFactory.DriveAllCars(Car.ThreadingModel.TaskThreads);
            CarFactory.WaitForAllCars();        // Waits for all cars to complete their trip
            winners = CarFactory.GetRaceWinners();
            foreach (Car c in winners)
            {
                Console.WriteLine($"{c.CarId} finished in position {c.FinishPosition}, with {c.NumberOfPitStopMade} pit stops");
            }


            Console.WriteLine("\r\n\r\n----------------------------------------------------------------------------------");

        }
    }

}
