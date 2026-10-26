// Select a server task and show its read-only preview.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
string[] tasks = { "Health report", "Inspect queue", "Preview cleanup" };

// Layout: construct the widgets shown on screen.
Rows layout = new Rows(
    new TextBlock("[bold]Server toolbox[/] [muted]· operations[/]"),
    new Select("Task", tasks.Select(task => new Choice(task))) { Label = "Task", Value = tasks[0] },
    new AppBinding(state =>
    {
        string task = state.Get<string>("Task", tasks[0]);
        string[] lines = task == tasks[0] ? new[]
        {
            "[pass]web-01 reachable[/]", "CPU 24% · memory 61%"
        }

        : task == tasks[1] ? new[] { "[accent]12 jobs waiting[/]", "Oldest job 4 minutes" }

        : new[]
        {
            "[warn]3 artifacts eligible[/]",
            "Eligible artifacts: build-cache, test-output, old-logs."
        };
        return new Panel()
        {
            Title = "Preview",
            Content = new Rows(lines.Select(line => new TextBlock(line)).ToArray())
        };
    }))
{
    Gap = 1
};

// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
