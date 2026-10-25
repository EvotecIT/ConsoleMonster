using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Table table = new Table()
    .Title("[bold]Nightly checks[/] [muted]· run summary[/]")
    .ShowSubtotals()
    .ShowTotals()
    .AddColumn("Target", c => c
        .IsKey()
        .Overflow(ColumnOverflow.TruncateMiddle)
        .MinWidth(12))
    .AddColumn("Result", c => c.Priority(10).Presenter(CellPresenters.Status))
    .AddColumn("Passed", c => c
        .Priority(8)
        .Presenter(CellPresenters.Number)
        .Aggregate(ColumnAggregate.Sum))
    .AddColumn("Failed", c => c
        .Priority(9)
        .Presenter(CellPresenters.Number)
        .Aggregate(ColumnAggregate.Sum)
        .AddRoleRules(RoleRule.Above(0, ThemeToken.Fail)))
    .AddColumn("Warnings", c => c
        .Priority(6)
        .Presenter(CellPresenters.Number)
        .Aggregate(ColumnAggregate.Sum)
        .AddRoleRules(RoleRule.Above(0, ThemeToken.Warn)))
    .AddColumn("Duration", c => c
        .Priority(4)
        .Presenter(CellPresenters.Duration)
        .Aggregate(ColumnAggregate.Sum))
    .AddColumn("Score", c => c
        .Priority(2)
        .Presenter(new DataBarPresenter(100, new NumberPresenter("%")))
        .Aggregate(ColumnAggregate.Average));
table.AddGroup("Domains");
table.AddValues("ad.adatum.com", StatusKind.Fail, 9, 2, 5, TimeSpan.FromSeconds(24), 56);
// <more>
table.AddValues("eu.ad.adatum.com", StatusKind.Fail, 13, 1, 1, TimeSpan.FromSeconds(16), 87);
table.AddGroup("Domain controllers");
table.AddValues("DC01.ad.adatum.com", StatusKind.Warn, 26, 0, 3, TimeSpan.FromSeconds(17), 90);
table.AddValues("DC02.ad.adatum.com", StatusKind.Warn, 23, 0, 6, TimeSpan.FromSeconds(17), 79);
table.AddValues("DC03.ad.adatum.com", StatusKind.Fail, 23, 3, 4, TimeSpan.FromSeconds(19), 77);
table.AddValues("DC07.eu.ad.adatum.com", StatusKind.Fail, 21, 4, 4, TimeSpan.FromSeconds(16), 72);
table.AddValues("DC09.eu.ad.adatum.com", StatusKind.Pass, 30, 0, 0, TimeSpan.FromSeconds(15), 100);
// </more>
Terminal.Write(table);