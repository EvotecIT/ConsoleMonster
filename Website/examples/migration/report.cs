using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

var servers = new[] {
    new { Name = "FS01", Site = "Warsaw", Pending = 4, Status = "Pass" },
    new { Name = "SQL01", Site = "Dublin", Pending = 12, Status = "Warn" },
    new { Name = "WEB01", Site = "Berlin", Pending = 0, Status = "Pass" }
};
Terminal.Write(Table.For(servers)
    .Column(s => s.Name)
    .Column(s => s.Site)
    .Column(s => s.Pending)
    .Column(s => s.Status, column => column.WithPresenter(CellPresenters.StatusWord))
    .WithTitle("Patch inventory"));
