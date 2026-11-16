using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Table table = new Table()
{
    Title = "[bold]Nightly checks[/] [muted]· run summary[/]",
    ShowSubtotals = true,
    ShowTotals = true
}.Column("Target", c =>
{
    c.IsKey = true;
    c.Overflow = ColumnOverflow.TruncateMiddle;
    c.MinWidth = 12;
}).Column("Result", c =>
{
    c.Priority = 10;
    c.Presenter = CellPresenters.Status;
}).Column("Passed", c =>
{
    c.Priority = 8;
    c.Presenter = CellPresenters.Number;
    c.Aggregate = ColumnAggregate.Sum;
}).Column("Failed", c =>
{
    c.Priority = 9;
    c.Presenter = CellPresenters.Number;
    c.Aggregate = ColumnAggregate.Sum;
    c.RoleRule(RoleRule.Above(0, ThemeToken.Fail));
}).Column("Warnings", c =>
{
    c.Priority = 6;
    c.Presenter = CellPresenters.Number;
    c.Aggregate = ColumnAggregate.Sum;
    c.RoleRule(RoleRule.Above(0, ThemeToken.Warn));
}).Column("Duration", c =>
{
    c.Priority = 4;
    c.Presenter = CellPresenters.Duration;
    c.Aggregate = ColumnAggregate.Sum;
}).Column("Score", c =>
{
    c.Priority = 2;
    c.Presenter = new DataBarPresenter(100, new NumberPresenter("%"));
    c.Aggregate = ColumnAggregate.Average;
});
table.Group("Domains");
table.Values("ad.adatum.com", StatusKind.Fail, 9, 2, 5, TimeSpan.FromSeconds(24), 56);
// <more>
table.Values("eu.ad.adatum.com", StatusKind.Fail, 13, 1, 1, TimeSpan.FromSeconds(16), 87);
table.Group("Domain controllers");
table.Values("DC01.ad.adatum.com", StatusKind.Warn, 26, 0, 3, TimeSpan.FromSeconds(17), 90);
table.Values("DC02.ad.adatum.com", StatusKind.Warn, 23, 0, 6, TimeSpan.FromSeconds(17), 79);
table.Values("DC03.ad.adatum.com", StatusKind.Fail, 23, 3, 4, TimeSpan.FromSeconds(19), 77);
table.Values("DC07.eu.ad.adatum.com", StatusKind.Fail, 21, 4, 4, TimeSpan.FromSeconds(16), 72);
table.Values("DC09.eu.ad.adatum.com", StatusKind.Pass, 30, 0, 0, TimeSpan.FromSeconds(15), 100);
// </more>
Terminal.Write(table);
