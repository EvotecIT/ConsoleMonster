// Show the same task data as plain text and a formatted table.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// The same synthetic objects feed both views.
var tasks = new[]
{
    new
    {
        Task = "Draft release notes",
        State = "Ready",
        Minutes = 12
    },
    new
    {
        Task = "Review examples",
        State = "In progress",
        Minutes = 25
    },
    new
    {
        Task = "Archive old drafts",
        State = "Waiting",
        Minutes = 8
    }
};
// Layout: compose the screen from the data above.
string baseline = "";
Table table = new Table().Expand(true);
table.AddColumn("Task");
table.AddColumn("State", column => column.Presenter(CellPresenters.StatusWord));
table.AddColumn("Minutes");
foreach (var task in tasks)
{
    if (baseline.Length > 0)
    {
        baseline += "\n";
    }

    baseline += $"{task.Task} | {task.State} | {task.Minutes} min";
    table.AddRow(task.Task, TableCell.FromValue(task.State), TableCell.FromValue(task.Minutes));
}

Rows layout = new Rows(new Rule("[bold]Everyday formatting[/] [muted]| same objects, clearer output[/]"), new TextBlock("[bold]Before[/] [muted]| plain text[/]"), new TextBlock(baseline), new TextBlock("[bold]After[/] [muted]| ConsoleMonster table[/]"), table, new TextBlock("[muted]Three tasks. Aligned columns and right-aligned numbers.[/]")).Gap(1);
// Behaviour: print the screen once and return.
layout.Show();
