// Show a readiness report with status checks and score bars.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Capabilities;
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

string separator = Terminal.GetCurrentProfile().Glyphs == GlyphTier.Ascii
    ? "|" : "·";

// Layout: compose the screen from the data above.
StatusList statuses = Ui.StatusList();
foreach (var check in checks)
{
    statuses.Add(check.Name, check.Status, check.Detail);
}

BarChart scores = Ui.BarChart().Maximum(100).Unit("%").Warning(80).Critical(50);
foreach (var area in areaScores)
    scores.Add(area.Name, area.Score);
Rows layout = Ui.Rows().Gap(1).Content(rowsContent =>
{
    rowsContent.Rule($"[accent bold]Readiness assessment[/] [muted]{separator} workspace[/]");
    rowsContent.Facts(facts => facts.Add(3, "passed", ThemeToken.Pass)
        .Add(1, "warning", ThemeToken.Warn)
        .Add(1, "failed", ThemeToken.Fail)
        .Add(5, "checks total"));
    rowsContent.Panel(p => p.Title("Checks").Content(panelContent =>
    {
        panelContent.Add(statuses);
    }));
    rowsContent.Panel(p => p.Title("Readiness by area").Content(panelContent =>
    {
        panelContent.Add(scores);
    }).HeaderRight("score / 100"));
    rowsContent.Markup(
        "[bold]Next:[/] rehearse a restore, then add the two missing test scenarios.");
    rowsContent.Markup($"[muted]Review owner: Platform team {separator} next review: Friday[/]");
});

// Behaviour: print the screen once and return.
layout.Show();

record CheckRow(string Name, StatusKind Status, string Detail);

record AreaScoreRow(string Name, int Score);
