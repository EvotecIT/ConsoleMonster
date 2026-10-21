using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleMonster.Examples.Extensions {
    /// <summary>Examples for logging, hosting and command-line tools: <c>logging</c>, <c>hosting [seconds]</c> or <c>cli [--servers N]</c>.</summary>
    public static class Program {
        /// <summary>Runs an example.</summary>
        /// <param name="args"><c>logging</c>, <c>hosting [seconds]</c> or <c>cli [--servers N]</c>.</param>
        /// <returns>The exit code.</returns>
        public static async Task<int> Main(string[] args) {
            string name = args.Length > 0 ? args[0].ToLowerInvariant() : "logging";
            switch (name) {
                case "logging":
                    Logging();
                    return 0;
                case "hosting":
                    int seconds = args.Length > 1 && int.TryParse(args[1], out int value) ? value : 12;
                    await HostingAsync(seconds).ConfigureAwait(false);
                    return 0;
                case "cli":
                    string[] rest = new string[Math.Max(0, args.Length - 1)];
                    Array.Copy(args, 1, rest, 0, rest.Length);
                    return CommandLineExample.Run(rest);
                default:
                    Console.Error.WriteLine("Use: logging | hosting [seconds] | cli [--servers N]");
                    return 1;
            }
        }

        // Every level, a scope, a structured message and an exception.
        private static void Logging() {
            using ILoggerFactory factory = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Trace).AddConsoleMonster());
            ILogger orders = factory.CreateLogger("Shop.Services.Orders");
            ILogger cache = factory.CreateLogger("Shop.Infrastructure.Cache");
            cache.LogTrace("Probing {Count} cache shards", 4);
            cache.LogDebug("Shard {Shard} answered in {Ms} ms", "eu-1", 12);
            using (orders.BeginScope("order {Id}", 1042)) {
                orders.LogInformation("Payment accepted: {Amount} EUR", 129.90m);
                orders.LogWarning("Stock is low for {Sku}: {Left} left", "A-17", 3);
            }
            try {
                throw new TimeoutException("The payment gateway did not answer within 30 s.");
            } catch (TimeoutException ex) {
                orders.LogError(ex, "Refund for order {Id} failed", 1038);
            }
            orders.LogCritical("Queue {Queue} is not draining", "orders-eu");
            orders.LogInformation("Done; {Processed} orders processed", 57);
        }

        // A generic host with three workers and the console dashboard; logs go into its log region.
        private static async Task HostingAsync(int seconds) {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ApplicationName = "Orders", EnvironmentName = "Staging" });
            builder.Logging.ClearProviders();
            builder.Logging.AddConsoleMonster();
            builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
            builder.Services.AddHostedService<QueueWorker>();
            builder.Services.AddHostedService<CacheWarmer>();
            builder.Services.AddHostedService<ReportBuilder>();
            builder.Services.AddConsoleDashboard(dashboard => dashboard.LogHeight = 6);
            using IHost host = builder.Build();
            using CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            await host.RunAsync(stop.Token).ConfigureAwait(false);
        }

        private sealed class QueueWorker : BackgroundService {
            private readonly ILogger<QueueWorker> _log;
            private readonly IConsoleDashboard _dashboard;

            public QueueWorker(ILogger<QueueWorker> log, IConsoleDashboard dashboard) {
                _log = log;
                _dashboard = dashboard;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                int order = 1040;
                while (!stoppingToken.IsCancellationRequested) {
                    order++;
                    _dashboard.SetDetail("Queue", (50 - order % 50) + " waiting");
                    _log.LogInformation("Picked up order {Order}", order);
                    if (order % 4 == 0) {
                        _log.LogWarning("Payment slow for order {Order}", order);
                    }
                    try {
                        await Task.Delay(900, stoppingToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        break;
                    }
                }
            }
        }

        private sealed class CacheWarmer : BackgroundService {
            private readonly ILogger<CacheWarmer> _log;

            public CacheWarmer(ILogger<CacheWarmer> log) {
                _log = log;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                await Task.Delay(2500, stoppingToken).ConfigureAwait(false);
                _log.LogInformation("Cache warmed: {Entries} entries", 12840);
            }
        }

        private sealed class ReportBuilder : BackgroundService {
            private readonly ILogger<ReportBuilder> _log;

            public ReportBuilder(ILogger<ReportBuilder> log) {
                _log = log;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                await Task.Delay(4000, stoppingToken).ConfigureAwait(false);
                _log.LogError("The report template folder is missing");
                throw new InvalidOperationException("The report template folder is missing.");
            }
        }
    }
}
