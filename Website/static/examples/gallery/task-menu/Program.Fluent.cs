// Select a server task and show its read-only preview.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: fictional inputs kept in memory.
string[] tasks = { "Health report", "Inspect queue", "Preview cleanup" };

// Layout: construct the widgets shown on screen.
Rows layout = Ui.Rows().Gap(1).Content(rowsContent2 =>
{
    rowsContent2.Markup("[accent bold]Server toolbox[/] [muted]· operations[/]");
    rowsContent2.Select(select => select.Key("Task")
        .AddChoices(tasks.Select(task => new Choice(task)))
        .Label("Task")
        .Value(tasks[0]));
    rowsContent2.Binding(binding => binding.Configure(o => o.Content = state =>
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
        return Ui.Panel().Title("Preview").Content(panelContent =>
        {
            panelContent.Rows(r => r.Content(rowsContent =>
            {
                foreach (var rowsContentWidget in (lines.Select(line => Ui.Markup(line))
                    .ToArray() ?? Array.Empty<IWidget>()))
                {
                    rowsContent.Add(rowsContentWidget);
                }
            }));
        });
    }));
});

// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
