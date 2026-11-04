using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One line for the whole run: a bar, counters, elapsed time and the time left.
var summary = new SummaryBar { Elapsed = TimeSpan.FromSeconds(42) }
    .SetProgress(30, 40)
    .SetCounter("passed", 27, StatusKind.Pass)
    .SetCounter("failed", 2, StatusKind.Fail)
    .SetCounter("skipped", 1, StatusKind.Skip);
Terminal.Write(summary);
