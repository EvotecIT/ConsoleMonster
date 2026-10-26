// Follow CPU and memory pressure in a bounded top-like dashboard.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: synthetic counters, not the machine's real process list.
var processes = new Process[]
{
    new("build-worker", 4102, 42, 860),
    new("test-runner", 4107, 28, 512),
    new("api-host", 4120, 12, 220),
    new("file-watcher", 4131, 3, 48)
};
double[] cpuHistory = { 38, 45, 51, 48, 56 };

// Layout: build a complete frame from one set of counters.
IWidget Frame(int tick)
{
    var rows = processes.Select(process => new ProcessRow(process.Name, process.Pid,
        process.Cpu + tick * 3, process.Memory + tick * 16))
        .ToArray();
    var table = Table.For(rows)
        .Column(row => row.Name)
        .Column(row => row.Pid, column => column.Presenter = CellPresenters.Identifier)
        .Column("CPU %", row => row.Cpu)
        .Column("Memory MiB", row => row.Memory);
    table.Expand = true;
    Sparkline history = new Sparkline
    {
        Label = "CPU %"
    };
    history.AddRange(cpuHistory.Concat(new[] { 56.0 + tick * 5 }));
    return new Rows(
        new Rule("[bold]Process monitor[/] [muted]· refresh 1s[/]"),
        new TextBlock(
            $"[accent]CPU {56 + tick * 5}%[/]   Memory {1640 + tick * 64} MiB   [muted]4 processes[/]"),
        table,
        new Panel() { Title = "CPU history", Content = history },
        new TextBlock("[muted]Ctrl+C stops monitoring. Final counters stay in scrollback.[/]"))
    {
        Gap = 1
    };
}

// Behaviour: update each frame in a bounded live run.
RegionWidget screen = new RegionWidget("processes", Frame(0));
using LiveSession session = new LiveSession(screen);
try
{
    session.Run(live =>
    {
        for (int tick = 0; tick <= 2 && !live.Cancellation.IsCancellationRequested; tick++)
        {
            screen.Handle(live).Set(Frame(tick));
            live.Delay(TimeSpan.FromMilliseconds(1000));
        }
    });
}
catch (OperationCanceledException cancelled)
    when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}

record Process(string Name, int Pid, int Cpu, int Memory);

record ProcessRow(string Name, int Pid, int Cpu, int Memory);
