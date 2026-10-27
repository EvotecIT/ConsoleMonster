// Watch a container host: state, restarts, CPU and memory against each limit, with an event feed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: four fictional samples, two seconds apart; no container engine is contacted.
var containers = new Container[]
{
    new(
        "web",
        "nginx:1.27",
        256,
        new[] { 8, 9, 12, 10 },
        new[] { 96, 98, 101, 99 },
        new[] { 0, 0, 0, 0 }),
    new(
        "api",
        "adatum/api:4.2",
        512,
        new[] { 34, 41, 3, 22 },
        new[] { 410, 468, 120, 190 },
        new[] { 2, 2, 3, 3 }),
    new(
        "worker",
        "adatum/worker:4.2",
        896,
        new[] { 55, 61, 72, 78 },
        new[] { 690, 742, 801, 846 },
        new[] { 0, 0, 0, 0 }),
    new(
        "db",
        "postgres:16",
        2048,
        new[] { 18, 22, 25, 21 },
        new[] { 1210, 1225, 1240, 1236 },
        new[] { 0, 0, 0, 0 }),
    new(
        "cache",
        "redis:7.4",
        512,
        new[] { 3, 4, 4, 3 },
        new[] { 152, 153, 153, 154 },
        new[] { 0, 0, 0, 0 }),
    new(
        "queue",
        "rabbitmq:3.13",
        1024,
        new[] { 6, 7, 9, 8 },
        new[] { 301, 305, 309, 312 },
        new[] { 1, 1, 1, 1 })
};
var events = new List<Event>();

// Layout: one complete screen per sample; restarts and memory pressure also go to the event feed.
IWidget Frame(int tick)
{
    string clock = $"10:00:{tick * 2:00}";
    var rows = new List<ContainerRow>();
    foreach (var container in containers)
    {
        int memoryPercent = (int)Math.Round(100.0 * container.Memory[tick] / container.Limit);
        bool restarted = tick > 0 && container.Restarts[tick] > container.Restarts[tick - 1];
        string status = restarted || memoryPercent >= 90
            ? "Fail"
            : memoryPercent >= 80 ? "Warn" : "Pass";
        if (restarted)
            events.Insert(
                0,
                new Event($"{clock} {container.Name}", StatusKind.Fail,
                    $"restarted after exit 137 · restart {container.Restarts[tick]}"));
        else if (memoryPercent >= 90)
            events.Insert(
                0,
                new Event($"{clock} {container.Name}", StatusKind.Warn,
                    $"memory at {memoryPercent}% of its {container.Limit} MiB limit"));
        rows.Add(new ContainerRow(container.Name, container.Image,
            restarted ? "restarting" : "running", container.Restarts[tick], container.Cpu[tick],
            container.Memory[tick], memoryPercent, status));
    }

    int restarting = rows.Count(row => row.State == "restarting");
    int memoryTotal = rows.Sum(row => row.Memory);
    int limitTotal = containers.Sum(container => container.Limit);
    var table = Ui.Table(rows)
        .Column(row => row.Container)
        .Column(row => row.Image)
        .Column(row => row.State, column => column.Presenter(CellPresenters.StatusWord))
        .Column(row => row.Restarts)
        .Column("CPU %", row => row.Cpu)
        .Column("Memory MiB", row => row.Memory)
        .Column("Memory %", row => row.MemoryPercent)
        .Column(row => row.Status, column => column.Presenter(CellPresenters.StatusWord));
    table.Expand();
    var cpu = Ui.Chart()
        .Type(ConsoleMonster.Widgets.ChartType.Bar)
        .WidgetId("cpu")
        .Maximum(100)
        .Warning(70)
        .Critical(90)
        .Build<BarChart>();
    var memory = Ui.Chart()
        .Type(ConsoleMonster.Widgets.ChartType.Bar)
        .WidgetId("memory")
        .Maximum(100)
        .Warning(80)
        .Critical(90)
        .Build<BarChart>();
    foreach (var row in rows)
    {
        cpu.Add(row.Container, row.Cpu);
        memory.Add(row.Container, row.MemoryPercent);
    }

    IWidget feed = Ui.Text().Content("[muted]No restarts or memory warnings yet.[/]").IsMarkup();
    if (events.Count > 0)
    {
        var list = Ui.StatusList();
        foreach (var item in events.Take(4))
            list.Add(item.Label, item.Status, item.Detail);
        feed = list;
    }

    return Ui.Rows().Gap(1).Content(c3 =>
    {
        c3.Rule(
            $"[bold]Containers[/] [muted]· docker-01.adatum.example.test · refresh 2s · {clock}[/]");
        c3.Text(
            c3Text => c3Text.Content(
                $"[pass]{rows.Count - restarting} running[/]   [warn]{restarting} restarting[/]   [muted]memory {memoryTotal:N0} of {limitTotal:N0} MiB reserved[/]")
                .IsMarkup());
        c3.Add(table);
        c3.Columns(cmColumns => cmColumns.Content(c2 =>
        {
            c2.Panel(cmPanel => cmPanel.Title("CPU %").Content(c =>
            {
                c.Add(cpu);
            }));
            c2.Panel(cmPanel => cmPanel.Title("Memory % of limit").Content(c =>
            {
                c.Add(memory);
            }));
        }));
        c3.Panel(cmPanel => cmPanel.Title("Events").Content(c =>
        {
            c.Add(feed);
        }));
    });
}

// Behaviour: build each sample's screen, then replay them in a bounded live dashboard.
var frames = Enumerable.Range(0, 4).Select(Frame).ToArray();
var screen = Ui.Region("containers").Content(c =>
{
    c.Add(frames[0]);
});
using LiveSession session = screen.CreateSession();
try
{
    session.Run(live =>
    {
        foreach (var frame in frames)
        {
            if (live.Cancellation.IsCancellationRequested)
                break;
            screen.Handle(live).Set(frame);
            live.Delay(TimeSpan.FromMilliseconds(2000));
        }
    });
}
catch (OperationCanceledException cancelled)
    when (cancelled.CancellationToken == session.Cancellation)
{
// Ctrl+C is a normal way to leave the dashboard, after the session restores the terminal.
}

record Container(string Name, string Image, int Limit, int[] Cpu, int[] Memory, int[] Restarts);

record ContainerRow(string Container, string Image, string State, int Restarts, int Cpu, int Memory,
    int MemoryPercent, string Status);

record Event(string Label, StatusKind Status, string Detail);
