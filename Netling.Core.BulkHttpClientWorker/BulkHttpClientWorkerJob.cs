using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core.BulkHttpClientWorker
{
    public class BulkHttpClientWorkerJob : IWorkerJob
    {
        private readonly int _index;
        private readonly Uri _uri;
        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _localStopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly IEnumerable<IPAddress> _ipRange;
        private readonly HttpClient _httpClient;
        private readonly StringContent _httpContentPayload;
        private readonly ConcurrentBag<Uri> _devicesUri;

        // Used to approximately calculate bandwidth
        private static readonly int MissingHeaderLength = "HTTP/1.1 200 OK\r\nContent-Length: 123\r\nContent-Type: text/plain\r\n\r\n".Length; 

        public BulkHttpClientWorkerJob(Uri uri, IEnumerable<IPAddress> ipRange)
        {
            _uri = uri;
            _ipRange = ipRange;
            _devicesUri = new ConcurrentBag<Uri>(ipRange.Select(ip => new Uri(uri, $"devices/{ip}/12500/test")));
            _httpContentPayload = new StringContent("2102F40C23060A00C35A9F22240F33353738303330343836363635333731011632048E001D4F30048002040022060A0099579F22360C100E06012204D3053447AE01");
        }

        private BulkHttpClientWorkerJob(
            int index, 
            Uri uri, 
            WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uri = uri;
            _stopwatch = Stopwatch.StartNew();
            _localStopwatch = new Stopwatch();
            _workerThreadResult = workerThreadResult;
            _httpClient = new HttpClient();
        }

        public async ValueTask DoWork()
        {
            _localStopwatch.Restart();

            if (_devicesUri.TryTake(out var uri))
            {
                using (var response = await _httpClient.PostAsync(uri, _httpContentPayload))
                {
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    var length = contentStream.Length + response.Headers.ToString().Length + MissingHeaderLength;
                    var responseTime = (float)_localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                    var statusCode = (int)response.StatusCode;

                    if (statusCode < 400)
                    {
                        _workerThreadResult.Add((int)_stopwatch.ElapsedMilliseconds / 1000, length, responseTime, statusCode, _index < 10);
                    }
                    else
                    {
                        _workerThreadResult.AddError((int)_stopwatch.ElapsedMilliseconds / 1000, responseTime, statusCode, _index < 10);
                    }
                }
                _devicesUri.Append(uri);
            }
            else
            {
                Console.WriteLine("There are no device available.");
            }
        }

        public WorkerThreadResult GetResults()
        {
            return _workerThreadResult;
        }

        public ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult)
        {
            return new ValueTask<IWorkerJob>(new BulkHttpClientWorkerJob(index, _uri, workerThreadResult));
        }
    }
}