using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One line for the whole run: a bar, counters, elapsed time and the time left.
var summary = new SummaryBar { Elapsed = TimeSpan.FromSeconds(42) }
    .Progress(30, 40)
    .Counter("passed", 27, StatusKind.Pass)
    .Counter("failed", 2, StatusKind.Fail)
    .Counter("skipped", 1, StatusKind.Skip);
Terminal.Write(summary);
