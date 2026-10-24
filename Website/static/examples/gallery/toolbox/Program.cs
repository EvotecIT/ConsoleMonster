// Select a server task and show its read-only preview.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
string[] tasks = { "Health report", "Inspect queue", "Preview cleanup" };
// Run options: redirected input selects a deterministic snapshot.
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: construct the widgets shown on screen.
Rows layout = new Rows(new TextBlock("[bold]Server toolbox[/] [muted]· safe local previews[/]"),
    new Select("Task", tasks.Select(task => new Choice(task))) { Label = "Task", Value = tasks[step] },
    new AppBinding(state => {
        string task = state.Get<string>("Task", tasks[0]);
        string[] lines = task == tasks[0] ? new[] { "[pass]web-01 reachable[/]", "Sample: CPU 24% · memory 61%" }
            : task == tasks[1] ? new[] { "[accent]12 jobs waiting[/]", "Sample: oldest job 4 minutes" }
            : new[] { "[warn]3 sample artifacts eligible[/]", "Preview only; no files are removed." };
        return new Panel(new Rows(lines.Select(line => new TextBlock(line)).ToArray()), "Preview");
    })) { Gap = 1 };
// Live updates: ConsoleApp handles input and reevaluates AppBinding as state changes.
// Escape exits; snapshot mode prints the selected state without reading keys.
ConsoleApp.Run(layout, options: new ConsoleAppOptions { StaticSnapshot = snapshot });
