using ConsoleMonster.Live;
using ConsoleMonster.Styling;

using (LiveRun run = new LiveRun(new[] {
    new RunItem("Read inventory", "node-01", "Inventory"),
    new RunItem("Check storage", "node-02", "Storage")
}, new LiveRunOptions {
    Title = "Fleet checks",
    ProgressInterval = TimeSpan.Zero,
    // The preview fixes time; omit Clock when monitoring real work.
    Clock = () => TimeSpan.Zero
})) {
    run.Started(0, "collecting");
    run.Finished(0, StatusKind.Pass, duration: TimeSpan.FromSeconds(1.2));
    run.Started(1, "evaluating");
    run.Finished(1, StatusKind.Warn, "Free space is below the preferred threshold",
        TimeSpan.FromSeconds(2.4));
    RunSnapshot summary = run.Complete();
}
