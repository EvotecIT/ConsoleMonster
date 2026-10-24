// Follow CPU and memory pressure in a bounded top-like dashboard.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: synthetic counters, not the machine's real process list.
var processes = new[] {
    new { Name = "build-worker", Pid = 4102, Cpu = 42, Memory = 860 },
    new { Name = "test-runner", Pid = 4107, Cpu = 28, Memory = 512 },
    new { Name = "api-host", Pid = 4120, Cpu = 12, Memory = 220 },
    new { Name = "file-watcher", Pid = 4131, Cpu = 3, Memory = 48 }
};
double[] cpuHistory = { 38, 45, 51, 48, 56 };

// Layout: build a complete frame from one set of counters.
IWidget Frame(int tick) {
    Table table = new Table { Expand = true };
    table.AddColumn("Name").AddColumn("Pid", column => column.Presenter = CellPresenters.Identifier)
        .AddColumn("CPU %").AddColumn("Memory MiB");
    foreach (var process in processes) {
        table.AddRow(process.Name, TableCell.FromValue(process.Pid), TableCell.FromValue(process.Cpu + tick * 3),
            TableCell.FromValue(process.Memory + tick * 16));
    }
    Sparkline history = new Sparkline { Label = "CPU %" };
    history.AddRange(cpuHistory.Concat(new[] { 56.0 + tick * 5 }));
    return new Rows(new Rule("[bold]Process monitor[/] [muted]· refresh 1s[/]"),
        new TextBlock($"[accent]CPU {56 + tick * 5}%[/]   Memory {1640 + tick * 64} MiB   [muted]4 processes[/]"), table,
        new Panel(history, "CPU history"),
        new TextBlock("[muted]Ctrl+C stops monitoring. Final counters stay in scrollback.[/]")) { Gap = 1 };
}

// Behaviour: update each frame in a bounded live run.
Slot screen = new Slot("processes", Frame(0));
using LiveSession session = new LiveSession(screen);
try {
    session.Run(live => {
        for (int tick = 0; tick <= 2 && !live.Cancellation.IsCancellationRequested; tick++) {
            screen.Handle(live).Set(Frame(tick));
            live.Delay(TimeSpan.FromMilliseconds(1000));
        }
    });
} catch (OperationCanceledException cancelled) when (cancelled.CancellationToken == session.Cancellation) {
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}
