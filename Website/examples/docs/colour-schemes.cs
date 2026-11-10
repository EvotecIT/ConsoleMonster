using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// The settings picker's preview: every status, a table, a progress bar, a chart and an error line.
DateTime when = new DateTime(2026, 11, 9, 14, 30, 0);
Table table = new Table { CardsBelow = 0 }
    .AddColumn("Check")
    .AddColumn("State")
    .AddColumn("Size", c =>
    {
        c.Presenter = CellPresenters.Bytes;
        c.Alignment = HorizontalAlignment.Right;
    })
    .AddColumn("Changed", c =>
    {
        c.Presenter = CellPresenters.Date;
        c.Priority = -1;
    });
table.AddRow("Backups", TableCell.ForStatus(StatusKind.Pass, "passed"),
    TableCell.FromValue(52_428_800L), TableCell.FromValue(when));
table.AddRow("Certificates", TableCell.ForStatus(StatusKind.Warn, "expiring"),
    TableCell.FromValue(4_096L), TableCell.FromValue(when.AddDays(-3)));
table.AddRow("Replication", TableCell.ForStatus(StatusKind.Fail, "failed"),
    TableCell.FromValue(1_288_490_189L), TableCell.FromValue(when.AddHours(-5)));
StatusList status = new StatusList()
    .Add("Build", StatusKind.Pass, "1 m 12 s")
    .Add("Tests", StatusKind.Warn, "2 skipped")
    .Add("Deploy", StatusKind.Fail, "timed out")
    .Add("Notify", StatusKind.Running);
BarChart chart = new BarChart().Add("web", 42).Add("sql", 87).Add("files", 23);
ProgressBar progress = new ProgressBar("Copying") { Value = 64, Total = 100 };
TextBlock error = new TextBlock(
    "[fail]Error:[/] [muted]Cannot reach[/] [accent]sql01.contoso.test[/]"
        + " [muted](port 1433)[/]");
Terminal.Write(new Rows(table, new Columns(status, new Rows(progress, chart)), error) { Gap = 1 });
