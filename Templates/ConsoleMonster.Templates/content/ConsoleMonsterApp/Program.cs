using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Hosting;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleMonsterApp;

/// <summary>
/// `dotnet run` opens the menu (or prints the report when the output is redirected);
/// `dotnet run -- serve` runs the generic host with the live console dashboard until Ctrl+C.
/// </summary>
public static class Program {
    public static async Task<int> Main(string[] args) {
        if (args.Length > 0 && args[0] == "serve") {
            await Service.RunAsync(args).ConfigureAwait(false);
            return 0;
        }
        if (Terminal.Profile.Mode != OutputMode.Interactive) {
            // Redirected, CI or a pipe: the report as text, no menu.
            Terminal.Write(Report.Build());
            return 0;
        }
        Menu menu = new Menu("ConsoleMonsterApp") { Description = "Pick an item with the arrow keys and Enter; Esc goes back." };
        menu.Item("Status report", item => {
            item.Description = "A table of services and a summary";
            item.Page(_ => Report.Build());
        });
        menu.Item("Run the service", item => {
            item.Description = "How to start the host with its live dashboard";
            item.Page(_ => new Panel(new TextBlock(
                "Run [accent]dotnet run -- serve[/] to start the generic host. The dashboard shows each hosted service, "
                + "its state and the log; Ctrl+C stops it.\n\nPublish one native executable with "
                + "[accent]dotnet publish -c Release -r win-x64[/] (or linux-x64, osx-arm64)."), "Service"));
        });
        menu.Exit();
        menu.Run();
        return 0;
    }
}

/// <summary>The status report: a table and a summary line.</summary>
public static class Report {
    public static IWidget Build() {
        Table table = new Table();
        table.AddColumn("Service");
        table.AddColumn("State");
        table.AddColumn("Uptime", HorizontalAlignment.Right);
        table.AddColumn("Requests", HorizontalAlignment.Right);
        table.AddRow("api", TableCell.ForStatus(StatusKind.Pass, "healthy"), "3d 4h", "1 204 311");
        table.AddRow("worker", TableCell.ForStatus(StatusKind.Warn, "slow"), "3d 4h", "88 412");
        table.AddRow("reports", TableCell.ForStatus(StatusKind.Fail, "stopped"), "0s", "0");
        return new Rows(
            new TextBlock("[bold]Services[/] [muted]· sample data[/]"),
            table,
            new TextBlock("[pass]2 running[/] · [fail]1 stopped[/]"));
    }
}

/// <summary>The generic host: logging through ConsoleMonster, one worker, the live console dashboard.</summary>
public static class Service {
    public static async Task RunAsync(string[] args) {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsoleMonster();
        builder.Services.AddHostedService<Worker>();
        // After AddHostedService: the dashboard lists the services registered before it.
        builder.Services.AddConsoleDashboard(dashboard => dashboard.Title = "ConsoleMonsterApp");
        await builder.Build().RunAsync().ConfigureAwait(false);
    }
}

/// <summary>A background job that reports its progress to the dashboard and the log.</summary>
public sealed class Worker : BackgroundService {
    private readonly ILogger<Worker> _log;
    private readonly IConsoleDashboard _dashboard;

    public Worker(ILogger<Worker> log, IConsoleDashboard dashboard) {
        _log = log;
        _dashboard = dashboard;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        int batch = 0;
        while (!stoppingToken.IsCancellationRequested) {
            batch++;
            _dashboard.SetDetail("Worker", "batch " + batch);
            _log.LogInformation("Processed batch {Batch}", batch);
            try {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            } catch (OperationCanceledException) {
                break;
            }
        }
    }
}
