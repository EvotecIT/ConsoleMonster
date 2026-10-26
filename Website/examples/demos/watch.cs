using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// The same synthetic queue snapshots as watch.ps1; a real provider supplies these rows instead.
ConsoleWatch.Run(
    tick =>
{
    string[] names =
    {
        "MX01",
        "MX02",
        "MX03",
        "MX04"
    };
    var rows = Enumerable.Range(0, names.Length).Select(index =>
    {
        int load = (tick * 7 + index * 3) % 11;
        return new
        {
            Server = names[index] + ".evotec.xyz",
            Queued = load * 4,
            Deferred = index == 2 ? 12 + tick * 5 : load % 3,
            Delivered = 1200 + tick * 85 + load * 9,
            Status = index == 2 && tick > 2 ? "Warn" : "Pass"
        };
    });
    return Table.For(rows);
},
    new WatchOptions { Title = "Mail queues", PollInterval = TimeSpan.FromSeconds(1.5), Count = 6, Differences = true }
);
