using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core;
using Netling.Core.BulkHttpClientWorker;
using Netling.Core.Models;

namespace Netling.M2M.ConsoleClient
{
    class Program
    {
        static readonly string Uri = @"http://localhost:36007/kingfisher";
        //static readonly string Uri = @"http://localhost:36004/ibis2";
        //static readonly string Uri = @"https://mk-qa-eu-aag-type1-1.m-kopa.net:36007/kingfisher";


        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

            RunWithDuration(Uri, 16, TimeSpan.FromSeconds(15)).Wait();
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);
        }

        private static Task RunWithDuration(string uri, int threads, TimeSpan duration)
        {
            Console.WriteLine(StartRunWithDurationString, duration.TotalSeconds, uri, threads);
            return Run(uri, threads, duration);
        }

        private static async Task Run(string uri, int threads, TimeSpan duration)
        {
            WorkerResult result;
            var worker = new Worker(new BulkHttpClientWorkerJob(uri, IPAddress.Parse("10.0.1.1").Range(100)));

            result = await worker.Run(uri.ToString(), threads, duration, new CancellationToken());

            Console.WriteLine(ResultString,
                result.Count,
                result.Elapsed.TotalSeconds,
                result.RequestsPerSecond,
                result.Bandwidth,
                result.Errors,
                result.Median,
                result.StdDev,
                result.Min,
                result.Max,
                GetAsciiHistogram(result));
        }

        private static string GetAsciiHistogram(WorkerResult workerResult)
        {
            if (workerResult.Histogram.Length == 0)
            {
                return string.Empty;
            }

            const string filled = "█";
            const string empty = " ";
            var histogramText = new string[7];
            var max = workerResult.Histogram.Max();

            foreach (var t in workerResult.Histogram)
            {
                for (var j = 0; j < histogramText.Length; j++)
                {
                    histogramText[j] += t > max / histogramText.Length * (histogramText.Length - j - 1) ? filled : empty;
                }
            }

            var text = string.Join("\r\n", histogramText);
            var minText = string.Format("{0:0.000} ms ", workerResult.Min);
            var maxText = string.Format(" {0:0.000} ms", workerResult.Max);
            text += "\r\n" + minText + new string('=', workerResult.Histogram.Length - minText.Length - maxText.Length) + maxText;
            return text;
        }

        private const string HelpString = @"
Usage: netling [-t threads] [-d duration] url

Options:
    -t count        Number of threads to spawn.
    -d count        Duration of the run in seconds.
    -c count        Amount of requests to send on a single thread.

Examples:
    netling http://localhost:5000/
    netling http://localhost:5000/ -t 8 -d 60
    netling http://localhost:5000/ -c 3000
";

        private const string StartRunWithCountString = @"
Running {0} test @ {1}";

        private const string StartRunWithDurationString = @"
Running {0}s test with {2} threads @ {1}";

        private const string ResultString = @"
{0} requests in {1:0.##}s
    Requests/sec:   {2:0}
    Bandwidth:      {3:0} mbit
    Errors:         {4:0}
Latency
    Median:         {5:0.000} ms
    StdDev:         {6:0.000} ms
    Min:            {7:0.000} ms
    Max:            {8:0.000} ms

{9}
";
    }
}
