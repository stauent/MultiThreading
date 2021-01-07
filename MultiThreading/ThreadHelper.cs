using System;
using System.Threading;

namespace MultiThreading
{
    ///---------------- Sample code shows how to use this class
    ///
    ///       // Now we will demonstrate how to execute threads using our ThreadHelper class.
    ///       // We can pass in ANY "Action" delegate (no parameters and no response). We can use
    ///       // any object method to matches the Action delegate signature. This also means we
    ///       // can use a lambda expression with a matching signature.
    ///       ThreadHelper.RunToCompletion(()=> { 
    ///               for(int i = 0; i < 10; ++i)
    ///               {
    ///               Console.WriteLine($"Thread printing {i}");
    ///               Thread.Sleep(500);
    ///               }
    ///           }
    ///       );
    ///
    ///       Console.WriteLine("About to execute thread that returns a value");
    ///
    ///       // This version of RunToCompletion takes 2 parameters. The first is the labda expression
    ///       // that will be executed. The second is the parameter that will be passed into the lamda expression.
    ///       int Total = ThreadHelper.RunToCompletion<List<int>, int>((inputList) => {
    ///           int TotalSofar = 0;
    ///           foreach(int i in inputList)
    ///           {
    ///               Console.WriteLine($"Thread printing intput value {i}");
    ///               Thread.Sleep(500);
    ///               TotalSofar += i;
    ///           }
    ///           return (TotalSofar);
    ///       }
    ///       // This is the "inputList" parameter for the "RunToCompletion" lambda expression
    ///       , new List<int> { 1,5,6,7,8}
    ///       );
    ///
    ///       Console.WriteLine($"RunToCompletion value returned is {Total}");
    ///
    ///----------------




    /// <summary>
    /// This class is used to wait for a result from a thread function
    /// </summary>
    /// <typeparam name="R">Specifies the return type of Result</typeparam>
    public class ThreadResult<R>
    {
        /// <summary>
        /// This is the wait handle used by the running thread.
        /// The "Result" property waits on this handle before returning the result.
        /// </summary>
        public AutoResetEvent WaitHandle { get; set; }
        
        /// <summary>
        /// Do not use this property to get result data. It is updated
        /// by the owning thread. It's value will only be set immediately
        /// before the thread returns.
        /// </summary>
        public R ResponseData { get; set; }

        /// <summary>
        /// Waits for the WaitHandle to become signalled
        /// and then returns the result of the function
        /// </summary>
        public R Result
        {
            get
            {
                WaitHandle.WaitOne();
                return (ResponseData);
            }
        }
    }


    /// <summary>
    /// The purpose of this class is to simplify starting
    /// a thread and waiting for it to complete. This is boilerplate
    /// code that the developer should not have to code over and over again.
    /// </summary>
    public static class ThreadHelper
    {
        /// <summary>
        /// This is the data used by RunToCompletion method that executes an Action delegate
        /// </summary>
        public class ThreadActionData
        {
            public Action ThreadActionDelegate { get; set; }
            public AutoResetEvent WaitHandle { get; set; }
        }

        /// <summary>
        /// Extention method that allows you to run any "Action" method 
        /// as a thread.
        /// </summary>
        /// <param name="ThreadActionDelegate">Action delegate specifying the thread code to be executed</param>
        public static void ExecuteAsThread(this Action ThreadActionDelegate)
        {
            RunToCompletion(ThreadActionDelegate);
        }


        /// <summary>
        /// This method will execute the code in the ThreadActionDelegate Action delegate
        /// and wait for it to complete. The calling thread will be suspended until
        /// the code in the Action delegate has completed. The thread will ALWAYS run
        /// as a background thread.
        /// </summary>
        /// <param name="ThreadActionDelegate">Action delegate specifying the thread code to be executed</param>
        public static void RunToCompletion(Action ThreadActionDelegate)
        {
            AutoResetEvent WaitHandle = new AutoResetEvent(false);

            // Start the thread and execute the code
            Thread t = new Thread(new ParameterizedThreadStart(ThreadHostWrapper));
            t.IsBackground = true;
            t.Start(new ThreadActionData { ThreadActionDelegate = ThreadActionDelegate, WaitHandle = WaitHandle });

            // Wait till thread signals that it has completed the operation
            WaitHandle.WaitOne();
        }

        /// <summary>
        /// This method will execute the code in the ThreadActionDelegate Action delegate
        /// and wait for it to complete. The calling thread will NOT wait for the Action delegate
        /// to complete. The thread will ALWAYS run as a background thread.
        /// </summary>
        /// <param name="ThreadActionDelegate">Action delegate specifying the thread code to be executed</param>
        /// <returns>AutoResetEvent used to WaitOne for the thread to finish</returns> 
        public static AutoResetEvent Run(this Action ThreadActionDelegate)
        {
            AutoResetEvent WaitHandle = new AutoResetEvent(false);

            // Start the thread and execute the code
            Thread t = new Thread(new ParameterizedThreadStart(ThreadHostWrapper));
            t.IsBackground = true;
            ThreadActionData data = new ThreadActionData { ThreadActionDelegate = ThreadActionDelegate, WaitHandle = WaitHandle };
            t.Start(data);

            return (data.WaitHandle);
        }


        /// <summary>
        /// Wrapper method used to execute thread code and to signal when code is complete
        /// </summary>
        /// <param name="Data">ThreadActionData containing delegate to execute and wait handle to be signalled when code is complete</param>
        private static void ThreadHostWrapper(object Data)
        {
            if (Data is ThreadActionData tData)
            {
                // Execute the code in the thread
                tData.ThreadActionDelegate();

                // Signal that the thread has completed
                tData.WaitHandle.Set();
            }
        }


        /// <summary>
        /// Used as a housing for the information the thread code needs to execute
        /// and then signal it has completed. This information is passed into the 
        /// RunToCompletion<T,R> method to provide it with the input data as well as 
        /// provide a mechanism to return response data out of the thread.
        /// </summary>
        private class ThreadFunctionData<T, R>: ThreadResult<R>
        {
            public Func<T, R> ThreadFunctionDelegate { get; set; }
            public T ParameterInputData { get; set; }
        }


        /// <summary>
        /// Executes a "Func" delegate as a thread. This method provides 1 input parameter
        /// to the "Func", executes the thread code and waits for the thread to end. It then
        /// returns the response data from the thread.
        /// </summary>
        /// <typeparam name="T">Input data is of type T</typeparam>
        /// <typeparam name="R">Reponse data is of type R</typeparam>
        /// <param name="ThreadFunctionDelegate">A delegate of type Func<T,R> </param>
        /// <param name="ParameterInputData">Parameter data for use by the ThreadFunctionDelegate</param>
        /// <returns>Response data from the thread (of type R)</returns>
        public static R ExecuteAsThread<T, R>(this Func<T, R> ThreadFunctionDelegate, T ParameterInputData)
        {
            return RunToCompletion<T, R>(ThreadFunctionDelegate, ParameterInputData);
        }

        /// <summary>
        /// Executes a "Func" delegate as a thread. This method provides 1 input parameter
        /// to the "Func", executes the thread code and waits for the thread to end. It then
        /// returns the response data from the thread.
        /// </summary>
        /// <typeparam name="T">Input data is of type T</typeparam>
        /// <typeparam name="R">Reponse data is of type R</typeparam>
        /// <param name="ThreadFunctionDelegate">A delegate of type Func<T,R> </param>
        /// <param name="ParameterInputData">Parameter data for use by the ThreadFunctionDelegate</param>
        /// <returns>Response data from the thread (of type R)</returns>
        public static R RunToCompletion<T, R>(Func<T, R> ThreadFunctionDelegate, T ParameterInputData)
        {
            AutoResetEvent WaitHandle = new AutoResetEvent(false);

            // Start the thread and execute the code
            Thread t = new Thread(new ParameterizedThreadStart(ThreadHostWrapper<T, R>));
            t.IsBackground = true;
            ThreadFunctionData<T, R> retVal = new ThreadFunctionData<T, R> { ThreadFunctionDelegate = ThreadFunctionDelegate, WaitHandle = WaitHandle, ParameterInputData = ParameterInputData, ResponseData = default(R) };
            t.Start(retVal);

            // Wait till thread signals that it has completed the operation
            WaitHandle.WaitOne();

            return (retVal.ResponseData);
        }

        /// <summary>
        /// Wrapper method used to execute thread code and to signal when code is complete
        /// </summary>
        /// <param name="Data">ThreadFunctionData containing delegate to execute and wait handle to be signalled when code is complete. ResponseData contains response from thread code</param>
        private static void ThreadHostWrapper<T, R>(object Data)
        {
            if (Data is ThreadFunctionData<T, R> tData)
            {
                // Execute the code in the thread
                tData.ResponseData = tData.ThreadFunctionDelegate(tData.ParameterInputData);

                // Signal that the thread has completed
                tData.WaitHandle.Set();
            }
        }

        /// <summary>
        /// Executes a "Func" delegate as a thread. This method provides 1 input parameter
        /// to the "Func", executes the thread code and DOES NOT wait for the thread to end. 
        /// The return value from the thread can be read using the "Result" extention method
        /// </summary>
        /// <typeparam name="T">Input data is of type T</typeparam>
        /// <typeparam name="R">Reponse data is of type R</typeparam>
        /// <param name="ThreadFunctionDelegate">A delegate of type Func<T,R> </param>
        /// <param name="ParameterInputData">Parameter data for use by the ThreadFunctionDelegate</param>
        /// <returns>Response data from the thread (of type R)</returns>
        public static ThreadResult<R> Run<T, R>(this Func<T, R> ThreadFunctionDelegate, T ParameterInputData)
        {
            AutoResetEvent WaitHandle = new AutoResetEvent(false);

            // Start the thread and execute the code
            Thread t = new Thread(new ParameterizedThreadStart(ThreadHostWrapper<T, R>));
            t.IsBackground = true;
            ThreadFunctionData<T, R> data = new ThreadFunctionData<T, R> { ThreadFunctionDelegate = ThreadFunctionDelegate, WaitHandle = WaitHandle, ParameterInputData = ParameterInputData, ResponseData = default(R) };
            t.Start(data);

            return (data);
        }
    }

}

