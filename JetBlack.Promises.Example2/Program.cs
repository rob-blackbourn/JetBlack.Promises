using System;
using System.Threading;

namespace JetBlack.Promises.Example2
{
    class Program
    {
        static void Main(string[] args)
        {
            SleepPromise(100)
                .Then(() => SleepPromise(100))
                .Then(() => SleepPromise(100))
                .Done(
                    () => Console.WriteLine("On resolved"),
                    error => Console.WriteLine("On rejected: {0}", error.Message));

            Console.WriteLine("Done");
        }

        static IPromise SleepPromise(int millis)
        {
            return new Promise((resolve, reject) =>
            {
                Console.WriteLine("Sleep: " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(millis);
                Console.WriteLine("Wake: " + Thread.CurrentThread.ManagedThreadId);
                resolve();
            });
        }
    }
}
