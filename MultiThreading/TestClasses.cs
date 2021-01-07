using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace MultiThreading
{
    public class TestClass
    {
        public void DoSomething()
        {
            for (int i = 0; i < 5; ++i)
            {
                Console.WriteLine($"DoSomething Thread printing {i}");
                Thread.Sleep(500);
            }
        }

        public int AddThese(List<int> NumbersToAdd)
        {
            int total = 0;
            foreach (int i in NumbersToAdd)
            {
                total += i;
            }
            return (total);
        }

        public int AddTheseWithWait(List<int> NumbersToAdd)
        {
            int total = 0;
            foreach (int i in NumbersToAdd)
            {
                total += i;
                Thread.Sleep(200);
                Console.WriteLine($"AddTheseWithWait Thread total so far {total}");
            }
            return (total);
        }


        public int AddThese(int first, int second)
        {
            return (first + second);
        }
    }
}
