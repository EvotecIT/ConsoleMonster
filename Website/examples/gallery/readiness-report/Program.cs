// Show a readiness report with status checks and score bars.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// Fixed sample results; --snapshot and --step 0 need no special handling.
// Replace these items with results you already have in memory.
var checks = new[] {
    (Name: "Configuration", Status: StatusKind.Pass, Detail: "All required settings present"),
    (Name: "Dependencies", Status: StatusKind.Pass, Detail: "No conflicting versions"),
    (Name: "Test coverage", Status: StatusKind.Warn, Detail: "Two scenarios need attention"),
    (Name: "Documentation", Status: StatusKind.Pass, Detail: "Examples match the current API"),
    (Name: "Recovery plan", Status: StatusKind.Fail, Detail: "Restore rehearsal still needed")
};
var areaScores = new[] {
    (Name: "Configuration", Score: 100), (Name: "Coverage", Score: 72), (Name: "Recovery", Score: 35)
};

// Layout: compose the screen from the data above.
StatusList statuses = new StatusList();
foreach (var check in checks) {
    statuses.Add(check.Name, check.Status, check.Detail);
}
BarChart scores = new BarChart { Maximum = 100, Unit = "%", Warning = 80, Critical = 50 };
foreach (var area in areaScores) scores.Add(area.Name, area.Score);

Rows layout = new Rows(
    new Rule("[bold]Readiness assessment[/] [muted]| workspace[/]"),
    new TextBlock("[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]"),
    new Panel(statuses, "Checks"),
    new Panel(scores, "Readiness by area") { HeaderRight = "score / 100" },
    new TextBlock("[bold]Next:[/] rehearse a restore, then add the two missing test scenarios."),
    new TextBlock("[muted]Review owner: Platform team · next review: Friday[/]")) { Gap = 1 };

// Live updates: this static screen prints once and returns immediately.
Terminal.Write(layout);
