using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Grid grid = new Grid("Services on FS01")
    .AddColumn(new TableColumn("Name") { IsKey = true })
    .AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status })
    .AddColumn("Start type")
    .AddColumn(new TableColumn("Memory") { Presenter = CellPresenters.Bytes })
    .AddRow("Spooler", "Spooler", StatusKind.Pass, "Automatic", 14_221_312L)
    // <more>
    .AddRow("MSSQLSERVER", "MSSQLSERVER", StatusKind.Pass, "Automatic", 812_466_176L)
    .AddRow("SQLSERVERAGENT", "SQLSERVERAGENT", StatusKind.Warn, "Manual", 42_004_480L)
    .AddRow("W32Time", "W32Time", StatusKind.Pass, "Automatic", 3_276_800L)
    .AddRow("WinRM", "WinRM", StatusKind.Pass, "Automatic", 9_830_400L)
    .AddRow("DFSR", "DFSR", StatusKind.Fail, "Automatic", 0L)
    .AddRow("LanmanServer", "LanmanServer", StatusKind.Pass, "Automatic", 6_553_600L);
// </more>
GridResult picked = grid.Show();
Terminal.WriteMarkupLine("Picked [bold]" + picked.Items.Count + "[/] services");
