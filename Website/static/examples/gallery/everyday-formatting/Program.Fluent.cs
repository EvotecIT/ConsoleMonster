// Show the same task data as plain text and a formatted table.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// The same synthetic objects feed both views.
var tasks = new TaskItem[]
{
    new("Draft release notes", "Ready", 12),
    new("Review examples", "In progress", 25),
    new("Archive old drafts", "Waiting", 8)
};
// Layout: compose the screen from the data above.
string baseline = "";
var table = Ui.Table(tasks).Column(row => row.Task).Column(row => row.State, column => column.Presenter(CellPresenters.StatusWord)).Column(row => row.Minutes);
table.Expand();
foreach (var task in tasks)
{
    if (baseline.Length > 0)
    {
        baseline += "\n";
    }

    baseline += $"{task.Task} | {task.State} | {task.Minutes} min";
}

Rows layout = Ui.Rows().Gap(1).Content(c =>
{
    c.Rule("[bold]Everyday formatting[/] [muted]| same objects, clearer output[/]");
    c.Text(cText => cText.Content("[bold]Before[/] [muted]| plain text[/]").IsMarkup());
    c.Text(cText => cText.Content(baseline).IsMarkup());
    c.Text(cText => cText.Content("[bold]After[/] [muted]| ConsoleMonster table[/]").IsMarkup());
    c.Add(table);
    c.Text(cText => cText.Content("[muted]Three tasks. Aligned columns and right-aligned numbers.[/]").IsMarkup());
});
// Behaviour: print the screen once and return.
layout.Show();
record TaskItem(string Task, string State, int Minutes);
