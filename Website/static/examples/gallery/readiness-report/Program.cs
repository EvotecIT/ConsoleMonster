// Show a readiness report with status checks and score bars.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fixed synthetic results; no checks run on this machine.
// Replace these items with results you already have in memory.
var checks = new CheckRow[]
{
    new("Configuration", StatusKind.Pass, "All required settings present"),
    new("Dependencies", StatusKind.Pass, "No conflicting versions"),
    new("Test coverage", StatusKind.Warn, "Two scenarios need attention"),
    new("Documentation", StatusKind.Pass, "Examples match the current API"),
    new("Recovery plan", StatusKind.Fail, "Restore rehearsal still needed")
};
var areaScores = new AreaScoreRow[]
{
    new("Configuration", 100),
    new("Coverage", 72),
    new("Recovery", 35)
};

// Layout: compose the screen from the data above.
StatusList statuses = new StatusList();
foreach (var check in checks)
{
    statuses.Add(check.Name, check.Status, check.Detail);
}

BarChart scores = new BarChart
{
    Maximum = 100,
    Unit = "%",
    Warning = 80,
    Critical = 50
};
foreach (var area in areaScores)
    scores.Add(area.Name, area.Score);
Rows layout = new Rows(
    new Rule("[bold]Readiness assessment[/] [muted]| workspace[/]"),
    new TextBlock(
        "[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]"),
    new Panel() { Title = "Checks", Content = statuses },
    new Panel() { Title = "Readiness by area", Content = scores, HeaderRight = "score / 100" },
    new TextBlock("[bold]Next:[/] rehearse a restore, then add the two missing test scenarios."),
    new TextBlock("[muted]Review owner: Platform team · next review: Friday[/]"))
{
    Gap = 1
};

// Behaviour: print the screen once and return.
Terminal.Write(layout);

record CheckRow(string Name, StatusKind Status, string Detail);

record AreaScoreRow(string Name, int Score);
