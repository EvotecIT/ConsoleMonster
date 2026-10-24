---
title: Large runs
slug: large-runs
order: 10
description: Stream finished work into scrollback while a bounded dashboard keeps active items and problems visible.
summary: Follow hundreds of checks, files or deployments without fitting every finished item on one screen.
group: live
card: large-run-120
apis: [LiveRun, RunDashboard, RunTracker]
chips: [Scrollback, Concurrent workers, CI]
---

`LiveRun` monitors work scheduled by your application or script. Each finished item prints once above an inline dashboard, so completed work stays in normal terminal scrollback. The dashboard occupies at most 40% of terminal height by default.

{{< cm-frame ids="large-run-120,large-run-60" >}}

The frames use illustrative checks with eight active workers. Their [C# scene](https://github.com/EvotecIT/ConsoleMonster/blob/master/ConsoleMonster.Examples/LargeRunScene.cs) is also used by the golden-frame tests.

## PowerShell

Declare the items, report state changes, then complete the run in `finally`. The monitor leaves your script's output and exit code alone.

```powershell
$files = @(Get-ChildItem *.log)
$run = New-ConsoleRun -Items $files -Title 'Archive logs' -Name Name -Target DirectoryName
try {
    for ($i = 0; $i -lt $files.Count; $i++) {
        Update-ConsoleRun $run -Index $i -State Running -Phase 'archiving'
        try {
            Compress-Archive $files[$i].FullName "$($files[$i].FullName).zip" -ErrorAction Stop
            Update-ConsoleRun $run -Index $i -State Pass
        } catch {
            Update-ConsoleRun $run -Index $i -State Fail -Message $_.Exception.Message
        }
    }
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
```

`-Name`, `-Target` and `-Group` accept property names or script blocks such as `-Group { $_.Directory.Name }`. Item positions start at zero. `Running` updates can change the phase without restarting elapsed time. `Pass`, `Fail`, `Warn` and `Skip` finish an item once.

## C#

Use the same monitor around your existing runner. Its methods accept calls from worker threads; it does not schedule work or collect application results.

```csharp
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

using (LiveRun run = new LiveRun(new[] {
    new RunItem("Read inventory", "node-01", "Inventory"),
    new RunItem("Check storage", "node-02", "Storage")
}, new LiveRunOptions { Title = "Fleet checks" })) {
    run.Dashboard.WithShowTrend(true).WithShowGroups(true);
    run.Started(0, "collecting");
    run.Finished(0, StatusKind.Pass);
    run.Started(1, "evaluating");
    run.Finished(1, StatusKind.Warn, "Free space is below the preferred threshold");
    RunSnapshot summary = run.Complete();
}
```

## Height, output and controls

The header shows the progress bar, counts, elapsed time, smoothed ETA and throughput. Running rows show names, targets, phases and elapsed time. The next queued items and optional groups use spare height. Warnings and failures stay pinned. In a short terminal, groups disappear first, queued rows collapse to a count, and active rows collapse to the worker count.

At completion, the dashboard is removed and the full summary prints into scrollback: totals, groups, every problem with wrapped detail, and the optional report path. Terminal scrollback retention is controlled by the terminal application.

CI and redirected output use append-only lines. `-NoLive` forces this mode. `-ProgressEvery` and `-ProgressSeconds` control compact progress updates. C# exposes the same settings through `LiveRunOptions`. `-HeightPercent` changes the dashboard limit.

Background workers can call `$run.Started($index, $phase)` and `$run.Finished($index, $state, $message)` directly. Use `Wait-ConsoleRun $run` on the calling pipeline while those workers run: PowerShell hosts receive timed progress through that command's output pump. Join workers before `Complete-ConsoleRun`, including during cancellation. In C#, observe `run.Cancellation`, let workers report their last outcomes, then call `Complete`.

`-InteractiveKeys` enables `p` to pause/resume completion lines, `f` to filter new log lines to problems, `g` to show/hide groups, and `?` for key help. Work and dashboard counts continue while the log is paused. Held lines flush on resume or completion. `-Trend` adds a throughput sparkline when width permits. Without keyboard input these controls are omitted.

For scheduling work directly, see [live runs](/docs/live-runs/). For building your own composition, see [live sessions](/docs/live-sessions/).
