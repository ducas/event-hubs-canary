using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventHubs.Canary.Console
{
    public static class Program
    {
        private static IConfiguration Configuration { get; set; }
        private static IServiceProvider ServiceProvider { get; set; }

        static void Main()
        {
            Configuration = BuildConfiguration();
            ServiceProvider = BuildServiceProvider();

            var cts = new CancellationTokenSource();

            System.Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
            
            ServiceProvider
                .GetService<Probe>()
                .Begin(cts.Token)
                .GetAwaiter()
                .GetResult();
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton(Configuration)
                .AddHttpClient()
                .AddLogging(b => b.AddConsole().AddConfiguration(Configuration.GetSection("Logging")))
                .AddSingleton<Probe>()
                .AddScoped<IClient, EventHubHttpClient>()
                .BuildServiceProvider();
        }
    }
}
