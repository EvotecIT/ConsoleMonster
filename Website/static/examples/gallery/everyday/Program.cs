using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// The same synthetic objects feed both views. Static arguments can be ignored.
var tasks = new[] {
    new { Task = "Draft release notes", State = "Ready", Minutes = 12 },
    new { Task = "Review examples", State = "In progress", Minutes = 25 },
    new { Task = "Archive old drafts", State = "Waiting", Minutes = 8 }
};
string baseline = "";
Table table = new Table { Expand = true };
table.AddColumn("Task");
table.AddColumn("State", column => column.Presenter = CellPresenters.StatusWord);
table.AddColumn("Minutes");
foreach (var task in tasks) {
    if (baseline.Length > 0) {
        baseline += "\n";
    }
    baseline += $"{task.Task} | {task.State} | {task.Minutes} min";
    table.AddRow(task.Task, TableCell.FromValue(task.State), TableCell.FromValue(task.Minutes));
}

Terminal.Write(new Rows(
    new Rule("[bold]Everyday formatting[/] [muted]| same objects, clearer output[/]"),
    new TextBlock("[bold]Before[/] [muted]| plain text[/]"),
    new TextBlock(baseline),
    new TextBlock("[bold]After[/] [muted]| ConsoleMonster table[/]"),
    table,
    new TextBlock("[muted]Three sample tasks. Aligned columns and right-aligned numbers.[/]")) { Gap = 1 });
