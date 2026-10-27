// Summarise a day of Windows event logs: trend, noisy sources, top IDs and the critical events.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional counts already grouped from the System and Application logs of six servers.
double[] errorsPerHour =
{
    4, 3, 5, 2, 3, 4, 6, 9, 14, 11, 8, 7, 6, 9, 31, 44, 27, 12, 8, 6, 5, 4, 6, 5
};
double[] warningsPerHour =
{
    12, 10, 11, 9, 10, 12, 15, 22, 30, 26, 21, 19, 18, 20, 41, 52, 38, 24, 19, 16, 14, 13, 15, 14
};
var sources = new SourceCount[]
{
    new("Service Control Manager", 74),
    new("Microsoft-Windows-DNS-Client", 52),
    new("MSSQLSERVER", 41),
    new("Schannel", 23),
    new("Disk", 17),
    new("Kernel-Power", 4)
};
var eventIds = new EventIdCount[]
{
    new(7031, 48, "Service terminated unexpectedly"),
    new(1014, 52, "Name resolution timed out"),
    new(17806, 37, "SSPI handshake failed"),
    new(36887, 23, "Fatal alert received"),
    new(153, 17, "IO operation retried")
};
var critical = new CriticalEvent[]
{
    new("14:52", "sql-01", "Kernel-Power", 41, "Rebooted without a clean shutdown"),
    new("15:07", "sql-01", "MSSQLSERVER", 824, "Logical consistency I/O error on tempdb"),
    new("15:31", "fs-02", "Disk", 7, @"Bad block on \Device\Harddisk1\DR1"),
    new("16:10", "web-03", "Kernel-Power", 41, "Rebooted without a clean shutdown")
};

// Behaviour: hour 0 is midnight; the busiest hour and the totals come from the same counts the charts use.
double errorTotal = errorsPerHour.Sum();
double warningTotal = warningsPerHour.Sum();
int peak = Array.IndexOf(errorsPerHour, errorsPerHour.Max());
var topIds = eventIds.OrderByDescending(row => row.Count).ToArray();

// Layout: headline, the 24-hour trend, the noisiest sources beside the top IDs, then criticals.
var errors = Ui.Chart()
    .Type(ConsoleMonster.Widgets.ChartType.Sparkline)
    .Title("errors")
    .Height(3)
    .Role(ThemeToken.Fail)
    .Build<Sparkline>();
errors.AddRange(errorsPerHour);
var warnings = Ui.Chart()
    .Type(ConsoleMonster.Widgets.ChartType.Sparkline)
    .Title("warnings")
    .Height(3)
    .Role(ThemeToken.Warn)
    .Build<Sparkline>();
warnings.AddRange(warningsPerHour);
var noisiest = Ui.Chart().Type(ConsoleMonster.Widgets.ChartType.Bar).Build<BarChart>();
foreach (var source in sources)
    noisiest.Add(source.Source, source.Errors);
var idTable = Ui.Table(topIds)
    .Column(row => row.Id, column => column.Presenter(CellPresenters.Identifier))
    .Column(row => row.Count)
    .Column(row => row.Meaning);
idTable.Border(TableBorder.None);
idTable.Expand();
var criticalTable = Ui.Table(critical)
    .Column(row => row.Time)
    .Column(row => row.Host)
    .Column(row => row.Source)
    .Column(row => row.Id, column => column.Presenter(CellPresenters.Identifier))
    .Column(row => row.Message);
criticalTable.Border(TableBorder.None);
criticalTable.Expand();
var report = Ui.Rows().Gap(1).Content(c3 =>
{
    c3.Rule("[bold]Event log summary[/] [muted]· 6 servers · Sunday 2026-10-25[/]");
    c3.Text(
        c3Text => c3Text.Content(
            $"[fail]{critical.Length} critical[/]   [warn]{errorTotal} errors[/]   [muted]{warningTotal} warnings · busiest hour {peak:00}:00[/]")
            .IsMarkup());
    c3.Columns(cmColumns => cmColumns.Content(c2 =>
    {
        c2.Panel(cmPanel => cmPanel.Title("Errors per hour").Content(c =>
        {
            c.Add(errors);
        }));
        c2.Panel(cmPanel => cmPanel.Title("Warnings per hour").Content(c =>
        {
            c.Add(warnings);
        }));
    }));
    c3.Columns(cmColumns => cmColumns.Content(c2 =>
    {
        c2.Panel(cmPanel => cmPanel.Title("Noisiest sources").Content(c =>
        {
            c.Add(noisiest);
        }));
        c2.Panel(cmPanel => cmPanel.Title("Top event IDs").Content(c =>
        {
            c.Add(idTable);
        }));
    }));
    c3.Panel(cmPanel => cmPanel.Title("Critical events").Content(c =>
    {
        c.Add(criticalTable);
    }));
});

// Behaviour: print once; feed it Get-WinEvent results grouped the same way for a real day.
report.Show();

record SourceCount(string Source, int Errors);

record EventIdCount(int Id, int Count, string Meaning);

record CriticalEvent(string Time, string Host, string Source, int Id, string Message);
