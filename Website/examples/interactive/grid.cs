using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Grid grid = new Grid("Services on FS01")
    .Column(new TableColumn("Name") { IsKey = true })
    .Column(new TableColumn("Status") { Presenter = CellPresenters.Status })
    .Column("Start type")
    .Column(new TableColumn("Memory") { Presenter = CellPresenters.Bytes })
    .Row("Spooler", "Spooler", StatusKind.Pass, "Automatic", 14_221_312L)
    // <more>
    .Row("MSSQLSERVER", "MSSQLSERVER", StatusKind.Pass, "Automatic", 812_466_176L)
    .Row("SQLSERVERAGENT", "SQLSERVERAGENT", StatusKind.Warn, "Manual", 42_004_480L)
    .Row("W32Time", "W32Time", StatusKind.Pass, "Automatic", 3_276_800L)
    .Row("WinRM", "WinRM", StatusKind.Pass, "Automatic", 9_830_400L)
    .Row("DFSR", "DFSR", StatusKind.Fail, "Automatic", 0L)
    .Row("LanmanServer", "LanmanServer", StatusKind.Pass, "Automatic", 6_553_600L);
// </more>
GridResult picked = grid.Show();
Terminal.WriteMarkupLine("Picked [bold]" + picked.Items.Count + "[/] services");
