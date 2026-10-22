using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

StatusList checks = new StatusList()
    .Add("Restore packages", StatusKind.Pass, "complete")
    .Add("Run smoke checks", StatusKind.Warn, "1 retry")
    .Add("Deploy api-west", StatusKind.Fail, "blocked")
    .Add("Publish report", StatusKind.Pending, "queued");

Table targets = new Table { Title = "Targets", Expand = true };
targets.AddColumn(new TableColumn("Target") { IsKey = true });
targets.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.StatusWord });
targets.AddColumn(new TableColumn("Detail") { Flexible = true });
targets.AddValues("api-east", "Pass", "Ready for rollout");
targets.AddValues("api-west", "Fail", "Health probe timed out");
targets.AddValues("worker-01", "Warn", "Restart scheduled");

LogRegion log = new LogRegion { PreferredHeight = 3 };
log.Write("Loaded deployment plan", "info", TimeSpan.FromSeconds(1));
log.Write("Smoke check needed one retry", "warn", TimeSpan.FromSeconds(3));
log.Write("api-west health probe timed out", "err", TimeSpan.FromSeconds(5));

Rows report = new Rows(
    new TextBlock("[accent]Build queue[/] [muted]sample data[/]"),
    new Panel(checks, "Checks"),
    targets,
    new Panel(log, "Recent log")) { Gap = 1 };

// Without a file argument, use the terminal's current theme.
if (args.Length == 0) {
    Terminal.Write(report);
} else {
    using (Terminal.Use(new ReportTarget(Terminal.Target, ThemeFile.Load(args[0]).ToTheme()))) {
        Terminal.Write(report);
    }
}

// Preserve the current destination and profile while applying this report's palette.
sealed class ReportTarget(ITerminalTarget destination, ITheme theme) : ITerminalTarget {
    public string Name => destination.Name;
    public TerminalProfile Profile => destination.Profile;
    public ITheme? Theme => theme;
    public ITerminalWriter? Writer => destination.Writer;
    public IInputSource? Input => destination.Input;
}