using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DomainController[] controllers =
{
    // <more>
    new("DC01.ad.adatum.com", "Warsaw", StatusKind.Pass, 12, TimeSpan.FromSeconds(42)),
    new("DC02.ad.adatum.com", "Warsaw", StatusKind.Warn, 140, TimeSpan.FromMinutes(42)),
    new("DC03.eu.ad.adatum.com", "Dublin", StatusKind.Pass, 31, TimeSpan.FromSeconds(64)),
    new("DC04.eu.ad.adatum.com", "Dublin", StatusKind.Fail, 0, TimeSpan.FromHours(4))
// </more>
};
var table = Table.For(controllers);
table.Title = "[bold]Domain controllers[/]";
table.Column(c => c.Name, c =>
{
    c.IsKey = true;
}).Column(c => c.Site, c =>
{
    c.Priority = 2;
}).Column(c => c.Status, c =>
{
    c.Presenter = CellPresenters.Status;
    c.Priority = 9;
}).Column(c => c.Latency, c =>
{
    c.Presenter = new NumberPresenter("ms");
    c.Priority = 5;
    c.AddRoleRules(RoleRule.Above(100, ThemeToken.Warn));
}).Column(c => c.LastSync, c =>
{
    c.Header = "Last sync";
    c.Presenter = CellPresenters.Duration;
    c.Priority = 4;
});
Terminal.Write(table);
record DomainController(string Name, string Site, StatusKind Status, int Latency, TimeSpan LastSync);
