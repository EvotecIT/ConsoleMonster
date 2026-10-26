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
StatusList statuses = Ui.StatusList();
foreach (var check in checks)
{
    statuses.Add(check.Name, check.Status, check.Detail);
}

BarChart scores = Ui.Chart().Type(ConsoleMonster.Widgets.ChartType.Bar).Maximum(100).Unit("%").Warning(80).Critical(50).Build<BarChart>();
foreach (var area in areaScores)
    scores.Add(area.Name, area.Score);
Rows layout = Ui.Rows().Gap(1).Content(c2 =>
{
    c2.Rule("[bold]Readiness assessment[/] [muted]| workspace[/]");
    c2.Text(c2Text => c2Text.Content("[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]").IsMarkup());
    c2.Panel(cmPanel => cmPanel.Title("Checks").Content(c =>
    {
        c.Add(statuses);
    }));
    c2.Panel(cmPanel => cmPanel.Title("Readiness by area").Content(c =>
    {
        c.Add(scores);
    }).HeaderRight("score / 100"));
    c2.Text(c2Text => c2Text.Content("[bold]Next:[/] rehearse a restore, then add the two missing test scenarios.").IsMarkup());
    c2.Text(c2Text => c2Text.Content("[muted]Review owner: Platform team · next review: Friday[/]").IsMarkup());
});
// Behaviour: print the screen once and return.
layout.Show();
record CheckRow(string Name, global::ConsoleMonster.Styling.StatusKind Status, string Detail);
record AreaScoreRow(string Name, int Score);
