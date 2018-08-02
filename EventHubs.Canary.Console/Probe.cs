using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EventHubs.Canary.Console
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Probe : IProbe
    {
        private readonly ILogger<Probe> _logger;
        private readonly IClient _client;

        public Probe(ILogger<Probe> logger, IClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task Begin(CancellationToken token)
        {
            _logger.LogInformation("Starting probe.");

            var count = 0;
            while (!token.IsCancellationRequested)
            {
                count++;
                var data = Encoding.UTF8.GetBytes(count.ToString());
                var partitionKey = (count % 100).ToString();
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await _client.SendAsync(data, partitionKey);
                    stopwatch.Stop();
                    _logger.LogDebug($"Sent message {count} in {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogCritical(new EventId(1, "Error sending event"), ex,
                        $"Failed to send message {count} to hub after {stopwatch.ElapsedMilliseconds}ms.");
                }

                var delay = TimeSpan.FromSeconds(1) - stopwatch.Elapsed;
                if (delay <= TimeSpan.Zero) continue;
                
                try
                {
                    await Task.Delay(delay, token);
                }
                catch (TaskCanceledException) { }
            }

            _logger.LogInformation("Canary removed from cage.");
        }
    }
}