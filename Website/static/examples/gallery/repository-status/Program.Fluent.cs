// Review several repositories without leaving the terminal.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: cached fictional git summaries; no git process or remote request runs here.
var repositories = new RepositoryStatus[]
{
    new("orbit-api", "feature/cache", 3, 1, 2, 0),
    new("orbit-cli", "main", 0, 0, 0, 4),
    new("orbit-docs", "docs/setup", 2, 0, 0, 0),
    new("orbit-tests", "main", 0, 0, 0, 0)
};

// Layout: numeric columns keep local changes separate from branch divergence.
var table = Ui.Table(repositories)
    .Column(row => row.Repository)
    .Column(row => row.Branch)
    .Column(row => row.Modified)
    .Column(row => row.Staged)
    .Column(row => row.Ahead)
    .Column(row => row.Behind)
    .Expand();
StatusList next = Ui.StatusList()
    .Add("orbit-api", StatusKind.Info, "Review the staged diff before committing")
    .Add("orbit-cli", StatusKind.Warn, "Working tree clean; inspect four incoming commits")
    .Add("orbit-docs", StatusKind.Info, "Two local edits need review")
    .Add("orbit-tests", StatusKind.Pass, "Clean and up to date");
Rows report = Ui.Rows().Gap(1).Content(c2 =>
{
    c2.Rule("[bold]Repository workspace[/] [muted]· ./work[/]");
    c2.Add(table);
    c2.Panel(p => p.Title("Suggested next steps").Content(c =>
    {
        c.Add(next);
    }));
});

// Behaviour: print once. Keep cached summaries fresh in a separate local collection step.
report.Show();

record RepositoryStatus(string Repository, string Branch, int Modified, int Staged, int Ahead,
    int Behind);
