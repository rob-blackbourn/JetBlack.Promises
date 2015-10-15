using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace JetBlack.Promises.Example1
{
    class Program
    {
        static void Main(string[] args)
        {
            var running = true;

            Download("http://intranet/Pages/home.aspx")   // Schedule an async operation.
                .Then(result =>                 // Use Done to register a callback to handle completion of the async operation.
                {
                    Console.WriteLine("Async operation completed.");
                    Console.WriteLine(result.Substring(0, 250) + "...");
                    running = false;
                })
                .Done(
                    s => Console.WriteLine("Resolved {0}", s),
                    error =>
                    {
                        Console.WriteLine("Rejected: {0}", error.Message);
                        running = false;
                    });

            Console.WriteLine("Waiting");

            while (running)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Exiting");
        }

        /// <summary>
        /// Download text from a URL.
        /// A promise is returned that is resolved when the download has completed.
        /// The promise is rejected if an error occurs during download.
        /// </summary>
        static IPromise<string> Download(string url)
        {
            Console.WriteLine("Downloading " + url + " ...");

            var promise = new Promise<string>();
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted +=
                    (s, ev) =>
                    {
                        if (ev.Error != null)
                        {
                            Console.WriteLine("An error occurred... rejecting the promise.");

                            // Error during download, reject the promise.
                            promise.Reject(ev.Error);
                        }
                        else
                        {
                            Console.WriteLine("... Download completed.");

                            // Downloaded completed successfully, resolve the promise.
                            promise.Resolve(ev.Result);
                        }
                    };

                client.DownloadStringAsync(new Uri(url), null);
            }
            return promise;
        }
    }
}
