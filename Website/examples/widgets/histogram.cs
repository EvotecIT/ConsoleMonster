using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Histogram durations = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 }).Height(5).Unit("s");
foreach (double seconds in new[] { 0.1, 0.2, 0.3, 0.3, 0.4, 0.6, 0.7, 0.7, 0.8, 0.9, 1.2, 1.4, 1.5, 1.9, 2.4, 3.1, 5.2 }) {
    durations.Add(seconds);
}
Terminal.Write(durations);