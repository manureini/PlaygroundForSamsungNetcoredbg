using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace TestDebugger
{
    class Program
    {
        private const string ProcessName = "TestPlayground";

        static void Main(string[] args)
        {
            Console.WriteLine("Wait 3s...");
            Thread.Sleep(3000);

            var process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (process == null)
                throw new Exception($"Process {ProcessName} not found");

            int pid = process.Id;

            DebugHelper.Attach(pid);

            Console.WriteLine("threads: ");
            var resp = DebugHelper.GetThreads();

            foreach (var thread in resp.Threads)
            {
                Console.WriteLine(thread.Id + " " + thread.Name);
            }

            DebugHelper.AddBreakPoint(21);
            DebugHelper.AddBreakPoint(22);

            Thread.Sleep(4000);

            DebugHelper.AddBreakPoint(24);

            Console.ReadLine();

            DebugHelper.Detach();
        }
    }
}
