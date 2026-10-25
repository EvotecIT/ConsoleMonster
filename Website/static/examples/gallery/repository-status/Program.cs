// Review several repositories without leaving the terminal.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: cached fictional git summaries; no git process or remote request runs here.
var repositories = new[]
{
    new
    {
        Repository = "orbit-api",
        Branch = "feature/cache",
        Modified = 3,
        Staged = 1,
        Ahead = 2,
        Behind = 0
    },
    new
    {
        Repository = "orbit-cli",
        Branch = "main",
        Modified = 0,
        Staged = 0,
        Ahead = 0,
        Behind = 4
    },
    new
    {
        Repository = "orbit-docs",
        Branch = "docs/setup",
        Modified = 2,
        Staged = 0,
        Ahead = 0,
        Behind = 0
    },
    new
    {
        Repository = "orbit-tests",
        Branch = "main",
        Modified = 0,
        Staged = 0,
        Ahead = 0,
        Behind = 0
    }
};
// Layout: numeric columns keep local changes separate from branch divergence.
Table table = new Table
{
    Expand = true
};
foreach (string column in new[]
{
    "Repository",
    "Branch",
    "Modified",
    "Staged",
    "Ahead",
    "Behind"
}

)
    table.AddColumn(column);
foreach (var repo in repositories)
{
    table.AddRow(repo.Repository, repo.Branch, TableCell.FromValue(repo.Modified), TableCell.FromValue(repo.Staged), TableCell.FromValue(repo.Ahead), TableCell.FromValue(repo.Behind));
}

StatusList next = new StatusList().Add("orbit-api", StatusKind.Info, "Review the staged diff before committing").Add("orbit-cli", StatusKind.Warn, "Working tree clean; inspect four incoming commits").Add("orbit-docs", StatusKind.Info, "Two local edits need review").Add("orbit-tests", StatusKind.Pass, "Clean and up to date");
Rows report = new Rows(new Rule("[bold]Repository workspace[/] [muted]· ./work[/]"), table, new Panel() { Title = "Suggested next steps", Content = next })
{
    Gap = 1
};
// Behaviour: print once. Keep cached summaries fresh in a separate local collection step.
Terminal.Write(report);
