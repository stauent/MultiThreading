using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreading
{
    /// <summary>
    /// Signature of the event handler that every car will call when it enters an intersection.
    /// </summary>
    /// <param name="carInIntersection">The car object entering the intersection</param>
    public delegate void IntersectionEvent(Car carInIntersection);

    public static class CarFactory
    {
        static int NextCarId { get; set; } = 1;
        static Car.DirectionOfTravel DirectionOfTravel = Car.DirectionOfTravel.NorthToSouth;

        // Maintains a list of all cars 
        static HashSet<Car> AllCars = new HashSet<Car>();

        // Maintains a list of all cars currently in the intersection
        static HashSet<Car> CarsInIntersection = new HashSet<Car>();

        // Lock object used to synchronize access to CarsInIntersection
        static object CarsInIntersectionLock = new object();


        /// <summary>
        /// Maintains the number of cars in the simulation
        /// </summary>
        static int NumberCarsInFleet = 0;

        /// <summary>
        /// Counts the number of cars that crashed during the simulation
        /// </summary>
        static int NumberOfCarsCrashed = 0;

        /// <summary>
        /// Counts the total number of times ANY car has entered the intersection
        /// </summary>
        static int IntersectionEventCounter = 0;

        /// <summary>
        /// As each car finishes the race, we assign the car a finish position
        /// </summary>
        static int FinishPosition = 1;

        /// <summary>
        /// Creates a car and initializes each one to give it a direction
        /// and an event handler to call when it enters an intersection.
        /// </summary>
        /// <param name="WaitAtStopSign">True indicates the car will obey stop signs, if false then it won't</param>
        /// <returns>Initialized car object</returns>
        private static Car CreateCar(bool WaitAtStopSign = false)
        {
            Car NextCar = new Car(NextCarId++, DirectionOfTravel, WaitAtStopSign);

            // Every car created will go in the opposite direction of the previous one
            if (DirectionOfTravel == Car.DirectionOfTravel.NorthToSouth)
            {
                DirectionOfTravel = Car.DirectionOfTravel.EastToWest;
            }
            else
            {
                DirectionOfTravel = Car.DirectionOfTravel.NorthToSouth;
            }

            // Set up event handler to process when a car has entered the intersection
            NextCar.Direction = DirectionOfTravel;
            NextCar.CarEnteredOrExitedIntersectionEvent += ProcessIntersectionEvent;

            // Add the car to the list of cars 
            AllCars.Add(NextCar);

            return (NextCar);
        }

        /// <summary>
        /// Waits for all cars to have their CarIsReadyToGo event in the signaled state. 
        /// </summary>
        public static void WaitForAllCars()
        {
            // Get the wait handle for each car
            EventWaitHandle[] CarsReady = new EventWaitHandle[CopyOfAllCars.Length];
            int CarIndex = 0;
            foreach (Car c in CopyOfAllCars)
            {
                CarsReady[CarIndex++] = c.CarIsReadyToGo;
            }

            // Wait for ALL cars to have their thread start before we let all 
            // cars start at the same time.
            WaitHandle.WaitAll(CarsReady);
        }

        /// <summary>
        /// Resets the CarIsReadyToGo event for all cars to the NON-signaled state  
        /// </summary>
        public static void ResetWaitAllCars()
        {
            foreach (Car c in CopyOfAllCars)
            {
                c.CarIsReadyToGo.Reset();
            }
        }

        /// <summary>
        /// Returns the list of cars in order that they finished the race
        /// </summary>
        /// <returns>IEnumerable<Car></returns>
        public static IEnumerable<Car> GetRaceWinners()
        {
            IEnumerable<Car> query = CopyOfAllCars.OrderBy(car => car.FinishPosition);
            return (query);
        }

        private static Car[] CopyOfAllCars { get; set; } = null;

        /// <summary>
        /// The AllCars is a hashset that is dynamically changing as cars
        /// crash or complete their trip. In order to have a list that will 
        /// not change for the duration of the simulation, a shallow copy
        /// of all cars is created for whatever processing needs to be
        /// performed on all cars.
        /// </summary>
        /// <returns></returns>
        public static Car[] CopyCars()
        {
            // Make a shallow copy of all cars because AllCars
            // might be modified before all cars start to drive
            int carCount = AllCars.Count();
            Car[] myCars = new Car[carCount];
            AllCars.CopyTo(myCars, 0, carCount);
            return (myCars);
        }

        /// <summary>
        /// Creates a fleet of cars that will be used in our traffic sumulation.
        /// Each car runs in its own thread.
        /// </summary>
        /// <param name="NumberOfCars">Number of cars to create for the simulation</param>
        /// <param name="WaitAtStopSign">If false, then cars DO NOT obey stop signs. If true, then they do.</param>
        public static void CreateFleet(int NumberOfCars = 20, bool WaitAtStopSign = false)
        {
            NumberCarsInFleet = NumberOfCars;
            NumberOfCarsCrashed = 0;
            FinishPosition = 1;

            for (int i = 0; i < NumberOfCars; ++i)
            {
                CreateCar(WaitAtStopSign);
            }

            // Make a copy of all cars becaue the AllCars hashset can change dynamically
            CopyOfAllCars = CopyCars();
        }

        /// <summary>
        /// This method starts all cars driving. It waits for all
        /// car threads to start but will not allow the cars to move
        /// until they are all ready. All the cars are told to start
        /// driving at the same time.
        /// </summary>
        /// <param name="UseThreadPool">If true, then a ThreadPool worker thread is used instead of creating a new one for each car</param>
        public static void DriveAllCars(Car.ThreadingModel Model = Car.ThreadingModel.ManualThreads)
        {
            // Use a shallow copy of all cars because AllCars
            // might be modified before all cars start to drive
            foreach (Car nextCar in CopyOfAllCars)
            {
                // Starts the thread that moves car
                nextCar.Drive(Model);

                // Wait a bit before starting the next car
                Random rnd = new Random();
                int waitSleep = rnd.Next(100, 201); // Creates a number between 100 and 200

                Thread.Sleep(waitSleep);
            }

            // Wait for all cars to be ready to go
            WaitForAllCars();

            // Reset the CarIsReadyToGo event on all cars to be non-signaled.
            // This will allow the code calling this "DriveAllCars" method to 
            // use the "WaitForAllCars" method to wait for all cars to complete
            // their trip (either crash or successfully finish).
            ResetWaitAllCars();

            // Tell all cars to start at the same time
            Car.StartAllCars.Set();
        }

        /// <summary>
        /// This is the event handler that ever car calls (synchronously) when
        /// the car is entering an intersection.
        /// </summary>
        /// <param name="carInIntersection"></param>
        static void ProcessIntersectionEvent(Car carInIntersection)
        {
            // Demonstrate how to make a single value thread safe without using
            // something like a "Monitor" which is more expensive.
            // Using Interlocked is better than using Monitor!
            //            lock (CarsInIntersectionLock)
            //            {
            //                IntersectionEventCounter++;
            //            }
            Interlocked.Increment(ref IntersectionEventCounter);

            // Prevent multiple cars corrupting the state of the current CarsInIntersection collection.
            // Once the current car has been processed, the other cars can be processed.
            // This lock does NOT prevent multiple cars from being in the intersection at the same.
            // It simply ensures that this method (ProcessIntersectionEvent) can only be executed
            // by one car at a time and will complete before the next car can be processed.
            //
            //  NOTE: "lock" is C# syntactic sugar that simply uses "Monitor" under the covers.
            //
            lock (CarsInIntersectionLock)
            {
                // If car has completed the trip, just remove it from the list
                if (carInIntersection.TripCompleted)
                {
                    // Tell the car it's finish position
                    carInIntersection.FinishPosition = FinishPosition++;

                    // This car is no longer available
                    AllCars.Remove(carInIntersection);

                    if (AllCars.Count() == 0)
                    {
                        Console.WriteLine($"{NumberCarsInFleet - NumberOfCarsCrashed} cars successfully completed their trip, while {NumberOfCarsCrashed} cars crashed during {IntersectionEventCounter} intersection events");
                    }

                    // Signal that this car has finished its trip. Once the last car has signaled the
                    // CarIsReadyToGo event, any code calling WaitForAllCars will unblock.
                    carInIntersection.CarIsReadyToGo.Set();
                }
                else
                {
                    // If car entering intersection, then check for crash
                    if (carInIntersection.InIntersection)
                    {
                        // Check if this car crashed while in the intersection
                        List<Car> CrashedCars = CheckForCrashInIntersection(carInIntersection);
                        if (CrashedCars.Count() == 0)
                        {
                            // If this car did not crash, then add it to one of the cars STILL in the intersection
                            CarsInIntersection.Add(carInIntersection);
                        }
                        else
                        {
                            // Tell each car involved in the pile up that it crashed and remove it from the intersection.
                            // If there are no more cars remaining after the crash, then display the message indicating this.
                            if (RemoveCrashedCars(CrashedCars) == 0)
                            {
                                Console.WriteLine($"{NumberCarsInFleet - NumberOfCarsCrashed} cars successfully completed their trip, while {NumberOfCarsCrashed} cars crashed during {IntersectionEventCounter} intersection events");
                            }

                            // Signal that all crashed cars have finished their trip. Once the last car has signaled the
                            // CarIsReadyToGo event, any code calling WaitForAllCars will unblock.
                            foreach (Car crashed in CrashedCars)
                            {
                                crashed.CarIsReadyToGo.Set();
                            }
                        }
                    }
                    else
                    {
                        // Car leaving intersection so remove from list of cars in intersection
                        // that other cars can crash into
                        CarsInIntersection.Remove(carInIntersection);
                    }
                }
            }
        }

        static int RemoveCrashedCars(List<Car> CrashedCars)
        {
            foreach (Car crashed in CrashedCars)
            {
                // This will cause the calling thread to stop driving the car
                crashed.CarCrashed = true;

                // This car can no longer fire events because it has crashed
                crashed.CarEnteredOrExitedIntersectionEvent -= ProcessIntersectionEvent;

                // This car has been removed from the intersection
                CarsInIntersection.Remove(crashed);

                // This car is no longer available
                AllCars.Remove(crashed);

                // Count one more crashed car
                ++NumberOfCarsCrashed;
            }

            return (AllCars.Count());
        }

        /// <summary>
        /// This method checks if the car entering the intersection will crash with
        /// any other cars already in the intersecion.
        /// </summary>
        /// <param name="carInIntersection">A car entering the intersection</param>
        /// <returns>List of all cars involved in a crash</returns>
        static List<Car> CheckForCrashInIntersection(Car carInIntersection)
        {
            // Crash ALL cars that are currently in the intersection
            // that are NOT going the same way as the current car that entered
            List<Car> CrashedCars = (from existingCarInIntersection in CarsInIntersection where existingCarInIntersection.Direction != carInIntersection.Direction select existingCarInIntersection).ToList();

            // The following code is equivalent to the previous LINQ expression
            //List<Car> CrashedCars = new List<Car>();
            //foreach (Car existingCarInIntersection in CarsInIntersection)
            //{
            //    if (existingCarInIntersection.Direction != carInIntersection.Direction)
            //    {
            //        CrashedCars.Add(existingCarInIntersection);
            //    }
            //}

            // If current car crashed with any other car, then add it to the list 
            // of crashed cars.
            if (CrashedCars.Count() > 0)
            {
                CarsInIntersection.Add(carInIntersection);
                CrashedCars.Add(carInIntersection);
            }

            return (CrashedCars);
        }
    }


    /// <summary>
    /// Maintains information about wind speed and direction
    /// </summary>
    public class Wind
    {
        public enum Direction
        {
            North,
            South,
            East,
            West
        }

        public int Speed { get; set; }
        public Direction WindDirection { get; set; }
        public Car.ThreadingModel Model { get; set; }

        public Wind(Car.ThreadingModel Model)
        {
            this.Model = Model;

            Random rnd = new Random();

            // Creates a number between 0 and 100
            Speed = rnd.Next(0, 101); 

            // Randomly specify a wind direction
            WindDirection = (Direction)rnd.Next(0, 3); 
        }

    }

    public class Car
    {
        /// <summary>
        /// AutoResetEvent is a synchronization method that will be used to ensure
        /// only 1 car enters the intersection at any point in time. This
        /// simulates a 4 way stop at an intersecion. When the car arrives
        /// at the intersection it will stop and wait (using WaitOne)
        /// until it is signaled that it is safe to enter (using Set)
        /// </summary>
        private static AutoResetEvent StopSign = new AutoResetEvent(true);

        /// <summary>
        /// If set to true, then the car will use the "StopSign" 
        /// AutoResetEvent to wait for the intersection to clear
        /// before proceeding into the intersection.
        /// </summary>
        private bool WaitAtStopSign { get; set; } = false;

        /// <summary>
        /// This event is used to block all cars from starting until
        /// "Set" is called. This mechanism can be used to simulate
        /// the start of a race where everyone must wait until
        /// the starter's pistol is fired. Once it's fired, ALL 
        /// cars start driving at the same time.
        /// </summary>
        public static ManualResetEvent StartAllCars = new ManualResetEvent(false);

        /// <summary>
        /// This event started in the non-signaled state. When the event is signaled
        /// it indicates that the thread this car runs on has been started. The
        /// code that starts all the cars will wait untill ALL cars have signaled
        /// that they are waiting to go.
        /// </summary>
        public AutoResetEvent CarIsReadyToGo { get; set; } = new AutoResetEvent(false);

        /// <summary>
        /// Max number of cars that can be in the Pit area at the same time
        /// </summary>
        public const int MaxCarsInPitstop = 3;

        /// <summary>
        /// Allow only MaxCarsInPitstop cars into the Pit area for servicing during the race
        /// </summary>
        public static Semaphore PitStop = new Semaphore(initialCount: MaxCarsInPitstop, maximumCount: MaxCarsInPitstop);

        /// <summary>
        /// Number of miles the car can travel before it needs a pit stop
        /// </summary>
        private int MilesTillPitStop { get; set; } = 40;

        /// <summary>
        /// Counts the number of times this car made a pit stop
        /// </summary>
        public int NumberOfPitStopMade { get; set; }

        /// <summary>
        /// Make of car
        /// </summary>
        public int CarId { get; set; }

        /// <summary>
        /// Number of milliseconds to sleep between movements. This value
        /// will be randomized between 50 and and 100 milliseconds.
        /// </summary>
        private int MovementSleep { get; set; } = 50;

        /// <summary>
        /// Each time the car makes a movement we count 1 mile
        /// </summary>
        private int MilesTravelled { get; set; } = 0;

        /// <summary>
        /// Specifies total length of the trip this car needs to make (in miles).
        /// This value will be randomized between 200 and 500.
        /// </summary>
        private int MilesLengthOfTrip { get; set; } = 500;

        /// <summary>
        /// Pretend the vehicle in travelling in a circle and will 
        /// hit the intersection every N miles. Every car will 
        /// pass the same intersection every N miles. This 
        /// will be between a random value between 10 and 50
        /// </summary>
        private int MilesTillIntersection { get; set; } = 50;


        /// <summary>
        /// Specifies the direction the car is travelling in. Cars
        /// travelling in the same direction can NEVER collide. 
        /// Cars can only collide with vehicles travelling in a different
        /// direction than their own.
        /// </summary>
        public DirectionOfTravel Direction { get; set; } = DirectionOfTravel.NorthToSouth;

        /// <summary>
        /// This is the event that will be fired when this car has entered or exited the intersection
        /// </summary>
        public event IntersectionEvent CarEnteredOrExitedIntersectionEvent;

        /// <summary>
        /// Specifies the direction the car is travelling in
        /// </summary>
        public enum DirectionOfTravel
        {
            NorthToSouth,
            EastToWest
        }

        public enum ThreadingModel
        {
            ManualThreads,
            ThreadPoolThreads,
            TaskThreads
        }

        /// <summary>
        /// When it is detected that the car is in the intersection
        /// this value will be true, and false otherwise. When
        /// in the intersection the car will fire the CarEnteredIntersection
        /// event.
        /// </summary>
        public bool InIntersection { get; set; } = false;

        /// <summary>
        /// Set to true if the car crahsed in the intersection
        /// </summary>
        public bool CarCrashed { get; set; } = false;

        /// <summary>
        /// Set to true when the trip has been completed
        /// </summary>
        public bool TripCompleted { get; set; } = false;

        /// <summary>
        /// Specifies the final position in which this car finished the race
        /// </summary>
        public int FinishPosition { get; set; }

        public Car(int CarId, DirectionOfTravel Direction = DirectionOfTravel.NorthToSouth, bool WaitAtStopSign = false)
        {
            this.CarId = CarId;
            this.WaitAtStopSign = WaitAtStopSign;

            Random rnd = new Random();
            MovementSleep = rnd.Next(50, 101);         // Creates a number between 50 and 100 for milliseconds to sleep between movements
            MilesLengthOfTrip = rnd.Next(200, 501);    // Creates a number between 200 and 500 for miles in trip to complete
            MilesTillIntersection = rnd.Next(10, 51);  // Creates a number between 10 and 50 for miles until this car reaches the intersection
            MilesTillPitStop = rnd.Next(10, 51);       // Creates a number between 10 and 50 for miles until this car needs a pit stop for servicing
        }


        /// <summary>
        /// This method creates a new thread and starts it in the background, OR 
        /// it uses an existing ThreadPool thread based on the input parameter.
        /// </summary>
        /// <param name="ThreadingModel">Specifies the threading model to use when executing the code</param>
        public void Drive(ThreadingModel Model = ThreadingModel.ManualThreads)
        {
            // Create a dummy object just to show how to pass a parameter into a thread
            Wind windy = new Wind(Model);

            switch (windy.Model)
            {
                case ThreadingModel.ManualThreads:
                    {
                        Thread backgroundThread = new Thread(new ParameterizedThreadStart(Go));
                        backgroundThread.Name = $"car {CarId}";
                        backgroundThread.IsBackground = true;
                        backgroundThread.Start(windy);
                    }
                    break;
                case ThreadingModel.ThreadPoolThreads:
                    {
                        // There is a cost with starting a new thread, so for purposes of efficiency, the thread pool holds onto created
                        // (but inactive) threads until needed. A ThreadPool thread is ALWAAYS a background thread.
                        WaitCallback workItem = new WaitCallback(Go);
                        ThreadPool.QueueUserWorkItem(workItem, windy);
                    }
                    break;
                case ThreadingModel.TaskThreads:
                    {
                        Task task = new Task(() => Go(windy));
                        task.Start();
                    }
                    break;
            }
        }

        /// <summary>
        /// This is the thread that will cause the car to move forward forever
        /// until it finishes its trip or crashes.
        /// </summary>
        /// <param name="ThreadParameter">Any parameter that the thread needs</param>
        protected void Go(object ThreadParameter)
        {
            if(ThreadParameter is Wind windy)
            {
                string ThreadTypeInfo = "";
                switch(windy.Model)
                {
                    case ThreadingModel.ManualThreads:
                        {
                            ThreadTypeInfo = $"Using new ParameterizedThreadStart thread {Thread.CurrentThread.ManagedThreadId}.";
                        }
                        break;
                    case ThreadingModel.ThreadPoolThreads:
                        {
                            ThreadTypeInfo = $"Using ThreadPool thread {Thread.CurrentThread.ManagedThreadId}.";
                        }
                        break;
                    case ThreadingModel.TaskThreads:
                        {
                            ThreadTypeInfo = $"Using TaskThreads thread {Thread.CurrentThread.ManagedThreadId}.";
                        }
                        break;
                }

                Console.WriteLine($"Started to drive car {CarId}, travelling in the {Direction.ToString()} direction, enter intersection every {MilesTillIntersection} miles.\r\nThe wind is coming out of the {windy.WindDirection.ToString()} at {windy.Speed} MPH.{ThreadTypeInfo}");
            }

            // Signal that this thread has started. We don't want to let
            // the cars start the race until ALL cars have started their
            // thread and are ready to go.
            CarIsReadyToGo.Set();

            // If the caller wants ALL cars to start at the same time
            // then all threads will block on the ManualResetEvent.
            // When "Set" is called, ALL threads/cars will start/unblock
            // at the same time
            StartAllCars.WaitOne();

            while ((MilesTravelled < MilesLengthOfTrip) && !CarCrashed)
            {
                MoveOneStep();
            }

            if (CarCrashed)
            {
                Console.WriteLine($"Vehicle {CarId} CRASHED in the intersection after {MilesTravelled} miles of a {MilesLengthOfTrip} mile trip");
            }
            else
            {
                Console.WriteLine($"Vehicle {CarId} successfully completed the {MilesLengthOfTrip} mile trip");

                // Create BOGUS event to have this car removed from list of all cars
                // because it has completed the trip.
                TripCompleted = true;
                CarEnteredOrExitedIntersectionEvent?.Invoke(this);
            }
        }

        /// <summary>
        /// Move the car forward one step and tell event listener when the car entered or exited the intersection
        /// </summary>
        protected void MoveOneStep()
        {
            ++MilesTravelled;
            Thread.Sleep(MovementSleep);

            // Check if this car needs a pit stop
            if ((MilesTravelled % MilesTillPitStop) == 0)
            {
                // Try to enter pit area. If there is a spot available
                // then the car can enter, otherwise it MUST wait for 
                // one of the cars already in the pit area to get out.
                PitStop.WaitOne();

                // Count the number of times the car has entered the pit area during the race.
                ++NumberOfPitStopMade;

                // Spend some time in the pit area
                Thread.Sleep(MovementSleep);

                // Leave the pit area to allow other cars to enter
                PitStop.Release();
            }

            // Check if we are in intersection
            if ((MilesTravelled % MilesTillIntersection) == 0)
            {
                // Wait at stop sign before entering the intersection
                if (WaitAtStopSign)
                {
                    StopSign.WaitOne();
                }

                InIntersection = true;

                // Indiate this car entered the intersection
                CarEnteredOrExitedIntersectionEvent?.Invoke(this);

                // If the car crashed, then it was already removed from the intersection
                if (!CarCrashed)
                {
                    // Car spends some time in the intersection
                    Thread.Sleep(MovementSleep);

                    InIntersection = false;

                    // Indiate this car left the intersection
                    CarEnteredOrExitedIntersectionEvent?.Invoke(this);
                }

                // Tell any other car waiting at the stop sign
                // that we exited the intersection and it's ok for them
                // to enter.
                if (WaitAtStopSign)
                {
                    StopSign.Set();
                }
            }
        }
    }

}
