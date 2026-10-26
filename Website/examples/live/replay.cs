using System.Globalization;
using ConsoleMonster.Building;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Replays a recorded assessment into its dashboard: every line of the recording is an update addressed to a widget
// by name, so the layout knows nothing about where the updates come from (a live run, or this file).
//   dotnet run -- assessment-run.jsonl 6     (the recording, and the speed)
string path = args.Length > 0 ? args[0] : "assessment-run.jsonl";
double speed = args.Length > 1 ? double.Parse(args[1], CultureInfo.InvariantCulture) : 1;
const int TotalRules = 332;
const int Workers = 6;

Phases phases = new Phases().Set("Discover").Set("Collect").Set("Evaluate").Set("Report");
StackedBar results = new StackedBar("results")
    .Add("Passed", 0, ThemeToken.Pass)
    .Add("Warnings", 0, ThemeToken.Warn)
    .Add("Failed", 0, ThemeToken.Fail)
    .Add("Skipped", 0, ThemeToken.Skip);
KeyValueList summary = new KeyValueList("summary")
    .Add("Evaluated", "0 / " + TotalRules)
    .Add("Average rate", "-")
    .Add("Remaining", "-")
    .Add("Targets done", "0 / 12")
    .Add("Targets failing", "0");
LineChart throughput = new LineChart("throughput").Height(7);
throughput.AddSeries("completed", ThemeToken.Accent);
throughput.AddSeries("failed", ThemeToken.Fail);
Histogram durations = new Histogram(
    new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 },
    "durations"
).Height(6).Unit("s");
BarChart findings = new BarChart("findings").Role(ThemeToken.Fail);
foreach (string category in new[] { "Domain controllers", "Replication", "DNS", "Kerberos", "Security", "Group Policy", "Accounts", "PKI" }) {
    findings.Add(category, 0);
}
Lanes targets = new Lanes { MaxLabelWidth = 28 };

Dashboard dashboard = new DashboardBuilder("[bold]Nightly checks[/]")
    .Subtitle("ad.adatum.com forest assessment · 12 targets · " + TotalRules + " rules")
    .Add(new Prioritized(new Padding(phases, 1, 0, 1, 1), 10, 1, 30))
    .Columns(c => c
        .Panel("Targets", new Prioritized(targets, 8, 4, 40), 2.3, Workers + " workers")
        .Panel("Results", new Rows(results, new Rule(), summary), 1))
    .Columns(c => c
        .Panel("Throughput", throughput, 1.2, "rules/s")
        .Panel("Rule duration", durations, 1)
        .Panel("Failures by category", findings, 1))
    .Panel("Log", new LogRegion { PreferredHeight = 6 }, fill: true)
    .BuildDashboard();
// Below 60 x 20 the dashboard shows one status line, the phases and the lanes that fit.
dashboard.MinimumSize = new Size(60, 20);
dashboard.CompactSummary = () => {
    double done = results.Segments.Sum(s => s.Value);
    double failed = results.Segments.First(s => s.Label == "Failed").Value;
    double warned = results.Segments.First(s => s.Label == "Warnings").Value;
    return done + "/" + TotalRules + " · [fail]✗" + failed + "[/] [warn]▴" + warned + "[/]";
};

using StreamReader recording = new StreamReader(path);
LiveSession.Replay(
    dashboard,
    recording,
    new LiveSessionOptions { Summary = new RunSummary(targets) },
    speed
);

// <more>
// A widget written by hand (IWidget): the run summary printed to the scrollback when the session ends.
sealed class RunSummary : IWidget {
    private readonly Lanes _lanes;

    public RunSummary(Lanes lanes) => _lanes = lanes;

    public Size Measure(Constraints constraints, RenderContext context) => Content().Measure(
        constraints,
        context
    );

    public void Render(Region region, RenderContext context) => Content().Render(region, context);

    private IWidget Content() {
        Table table = new Table()
            .Border(TableBorder.Minimal)
            .Title("[bold]Run summary[/]")
            .AddColumn("Target", c => c.Wrap(false))
            .AddColumn("Status")
            .AddColumn("Passed")
            .AddColumn("Failed")
            .AddColumn("Warnings")
            .AddColumn("Skipped")
            .AddColumn("Duration");
        int passed = 0, failed = 0, warned = 0, skipped = 0;
        foreach (Lane lane in _lanes.Items) {
            string duration = lane.Started.HasValue && lane.Finished.HasValue
                ? NumberFormat.Duration(lane.Finished.Value - lane.Started.Value)
                : "-";
            table.AddRow(
                Markup.Escape(lane.Label),
                TableCell.ForStatus(lane.Status),
                lane.Passed.ToString(),
                lane.Failed.ToString(),
                lane.Warned.ToString(),
                lane.Skipped.ToString(),
                duration
            );
            passed += lane.Passed;
            failed += lane.Failed;
            warned += lane.Warned;
            skipped += lane.Skipped;
        }
        table.SetFooter(
            "[bold]Total[/]",
            string.Empty,
            passed.ToString(),
            failed.ToString(),
            warned.ToString(),
            skipped.ToString(),
            string.Empty
        );
        return new Padding(table, 1, 1, 1, 1);
    }
}
// </more>
