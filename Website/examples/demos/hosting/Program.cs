using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// A generic host with three workers and the console dashboard; log entries go into its log region. Ctrl+C stops it.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ApplicationName = "Orders", EnvironmentName = "Staging" });
builder.Logging.ClearProviders();
builder.Logging.AddConsoleMonster();
// A worker that fails shows as failed on the dashboard while the others keep running.
builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
builder.Services.AddHostedService<QueueWorker>();
builder.Services.AddHostedService<CacheWarmer>();
builder.Services.AddHostedService<ReportBuilder>();
builder.Services.AddConsoleDashboard(dashboard => dashboard.LogHeight = 6);
await builder.Build().RunAsync();

sealed class QueueWorker(ILogger<QueueWorker> log, IConsoleDashboard dashboard) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        int order = 1040;
        while (!stoppingToken.IsCancellationRequested) {
            order++;
            dashboard.SetDetail("Queue", (50 - order % 50) + " waiting");
            log.LogInformation("Picked up order {Order}", order);
            if (order % 4 == 0) {
                log.LogWarning("Payment slow for order {Order}", order);
            }
            try {
                await Task.Delay(900, stoppingToken);
            } catch (OperationCanceledException) {
                break;
            }
        }
    }
}

sealed class CacheWarmer(ILogger<CacheWarmer> log) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Delay(2500, stoppingToken);
        log.LogInformation("Cache warmed: {Entries} entries", 12840);
    }
}

sealed class ReportBuilder(ILogger<ReportBuilder> log) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Delay(4000, stoppingToken);
        log.LogError("The report template folder is missing");
        throw new InvalidOperationException("The report template folder is missing.");
    }
}
