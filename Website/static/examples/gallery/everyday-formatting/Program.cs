// Show the same task data as plain text and a formatted table.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Capabilities;
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

string separator = Terminal.GetCurrentProfile().Glyphs == GlyphTier.Ascii
    ? "|" : "·";

// Layout: compose the screen from the data above.
string baseline = "";
var table = Table.For(tasks)
    .Column(row => row.Task)
    .Column(row => row.State, column => column.Presenter = CellPresenters.StatusWord)
    .Column(row => row.Minutes);
table.Expand = true;
foreach (var task in tasks)
{
    if (baseline.Length > 0)
    {
        baseline += "\n";
    }

    baseline += $"{task.Task} | {task.State} | {task.Minutes} min";
}

Rows layout = new Rows(
    new Rule(($"[accent bold]Everyday formatting[/] [muted]{separator} same objects, clearer "
        + $"output[/]")),
    TextBlock.FromMarkup($"[bold]Before[/] [muted]{separator} plain text[/]"),
    TextBlock.FromMarkup(baseline),
    TextBlock.FromMarkup($"[bold]After[/] [muted]{separator} ConsoleMonster table[/]"),
    table,
    TextBlock.FromMarkup("[muted]Three tasks. Aligned columns and right-aligned numbers.[/]"))
{
    Gap = 1
};

// Behaviour: print the screen once and return.
Terminal.Write(layout);

record TaskItem(string Task, string State, int Minutes);
