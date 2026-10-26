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
var table = Table.For(repositories)
    .Column(row => row.Repository)
    .Column(row => row.Branch)
    .Column(row => row.Modified)
    .Column(row => row.Staged)
    .Column(row => row.Ahead)
    .Column(row => row.Behind);
table.Expand = true;

StatusList next = new StatusList().Add(
    "orbit-api",
    StatusKind.Info,
    "Review the staged diff before committing"
)
    .Add("orbit-cli", StatusKind.Warn, "Working tree clean; inspect four incoming commits")
    .Add("orbit-docs", StatusKind.Info, "Two local edits need review")
    .Add("orbit-tests", StatusKind.Pass, "Clean and up to date");
Rows report = new Rows(
    new Rule("[bold]Repository workspace[/] [muted]· ./work[/]"),
    table,
    new Panel() { Title = "Suggested next steps", Content = next }
)
{
    Gap = 1
};
// Behaviour: print once. Keep cached summaries fresh in a separate local collection step.
Terminal.Write(report);
record RepositoryStatus(string Repository, string Branch, int Modified, int Staged, int Ahead, int Behind);
