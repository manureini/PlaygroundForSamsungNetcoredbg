using System;
using System.Diagnostics;
using System.Threading;

namespace TestPlayground
{
    public static class TestClass
    {
        static TestClass()
        {
            Console.WriteLine(nameof(TestClass) + " loaded");
        }

        public static void Run()
        {
            //  System.Diagnostics.Debugger.Launch();

            int i = 0;
            while (true)
            {
                Console.WriteLine(i++);
                Console.WriteLine("Debugger attached:" + Debugger.IsAttached); 
                //    Debugger.Break();
                Thread.Sleep(500);
            }
        }
    }
}
