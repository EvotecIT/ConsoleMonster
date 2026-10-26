// Select a server task and show its read-only preview.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: fictional inputs kept in memory.
string[] tasks = { "Health report", "Inspect queue", "Preview cleanup" };

// Layout: construct the widgets shown on screen.
Rows layout = Ui.Rows().Gap(1).Content(c3 =>
{
    c3.Text(c3Text => c3Text.Content("[bold]Server toolbox[/] [muted]· operations[/]").IsMarkup());
    c3.Select(cmSelect => cmSelect.Key("Task")
        .AddChoices(tasks.Select(task => new Choice(task)))
        .Label("Task")
        .Value(tasks[0]));
    c3.Binding(cmBinding => cmBinding.Configure(cmDefinition => cmDefinition.Content = state =>
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
        return Ui.Panel().Title("Preview").Content(c2 =>
        {
            c2.Rows(cmRows => cmRows.Content(c =>
            {
                foreach (var cWidget in (lines.Select(line => Ui.Text().Content(line).IsMarkup())
                    .ToArray()
                    ?? global::System.Array.Empty<global::ConsoleMonster.Layout.IWidget>()))
                {
                    c.Add(cWidget);
                }
            }));
        });
    }));
});

// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
