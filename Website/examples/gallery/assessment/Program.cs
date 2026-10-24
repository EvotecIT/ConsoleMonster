using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Fixed sample results; --snapshot and --step 0 need no special handling.
// Replace these items with results you already have in memory.
var checks = new[] {
    (Name: "Configuration", Status: StatusKind.Pass, Detail: "All required settings present"),
    (Name: "Dependencies", Status: StatusKind.Pass, Detail: "No conflicting versions"),
    (Name: "Test coverage", Status: StatusKind.Warn, Detail: "Two scenarios need attention"),
    (Name: "Documentation", Status: StatusKind.Pass, Detail: "Examples match the current API"),
    (Name: "Recovery plan", Status: StatusKind.Fail, Detail: "Restore rehearsal still needed")
};
StatusList statuses = new StatusList();
foreach (var check in checks) {
    statuses.Add(check.Name, check.Status, check.Detail);
}
BarChart scores = new BarChart { Max = 100, Unit = "%", Warning = 80, Critical = 50 };
scores.Add("Configuration", 100);
scores.Add("Coverage", 72);
scores.Add("Recovery", 35);

Terminal.Write(new Rows(
    new Rule("[bold]Readiness assessment[/] [muted]| sample workspace[/]"),
    new TextBlock("[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]"),
    new Panel(statuses, "Checks"),
    new Panel(scores, "Readiness by area") { Caption = "score / 100" },
    new TextBlock("[bold]Next:[/] rehearse a restore, then add the two missing test scenarios."),
    new TextBlock("[muted]Illustrative results only. No live assessment has been performed.[/]")) { Gap = 1 });
