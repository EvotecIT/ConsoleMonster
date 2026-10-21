using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DomainController[] controllers = {
    // <more>
    new("DC01.ad.adatum.com", "Warsaw", StatusKind.Pass, 12, TimeSpan.FromSeconds(42)),
    new("DC02.ad.adatum.com", "Warsaw", StatusKind.Warn, 140, TimeSpan.FromMinutes(42)),
    new("DC03.eu.ad.adatum.com", "Dublin", StatusKind.Pass, 31, TimeSpan.FromSeconds(64)),
    new("DC04.eu.ad.adatum.com", "Dublin", StatusKind.Fail, 0, TimeSpan.FromHours(4))
    // </more>
};

Terminal.Write(Table.For(controllers)
    .WithTitle("[bold]Domain controllers[/]")
    .Column(c => c.Name, c => c.WithIsKey())
    .Column(c => c.Site, c => c.WithPriority(2))
    .Column(c => c.Status, c => c.WithPresenter(CellPresenters.Status).WithPriority(9))
    .Column(c => c.Latency, c => c
        .WithPresenter(new NumberPresenter("ms"))
        .WithPriority(5)
        .AddToneRules(ToneRule.Above(100, ThemeToken.Warn)))
    .Column(c => c.LastSync, c => c
        .WithHeader("Last sync")
        .WithPresenter(CellPresenters.Duration)
        .WithPriority(4)));

record DomainController(string Name, string Site, StatusKind Status, int Latency, TimeSpan LastSync);