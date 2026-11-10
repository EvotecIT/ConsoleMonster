---
title: Large runs
slug: large-runs
order: 32
description: Stream finished work into scrollback while a bounded dashboard keeps active items and problems visible.
summary: Follow hundreds of checks, files or deployments without fitting every finished item on one screen.
group: live
card: large-run-120
apis: [LiveRun, RunDashboard, RunTracker]
chips: [Scrollback, Concurrent workers, CI]
---

`LiveRun` monitors work scheduled by your application or script. Each finished item prints once above an inline dashboard, so completed work stays in normal terminal scrollback. The dashboard occupies at most 40% of terminal height by default.

## Try it

This example reports two illustrative outcomes without contacting either target. Replace the comments and updates with your own checks. Choose PowerShell or C#; the output comes from the C# program with a fixed clock, while PowerShell shows the time your script takes.

{{< cm-try key="large-run" ids="large-run-example" project="FleetChecks" >}}

## How it works

Declare the items, report state changes, then complete the run in `finally`. The monitor leaves your script's output and exit code alone:

```powershell
$run = Start-ConsoleRun -InputObject $checks -Label Name -Target Target -Group { $_.Category }
try {
    Update-Console -Run $run -Index 0 -Status Running -Label 'collecting'
    Update-Console -Run $run -Index 0 -Status Pass
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
```

`-Label` and `-Group` accept property names or script blocks. Item positions start at zero. `Running` updates can change the phase without restarting elapsed time. `Pass`, `Fail`, `Warn` and `Skip` finish an item once. In C#, the handle's `Started` and `Finished` methods accept calls from worker threads.

The larger previews below show 214 illustrative checks with eight active workers partway through a run. Their [C# scene](https://github.com/EvotecIT/ConsoleMonster/blob/master/ConsoleMonster.Examples/LargeRunScene.cs) is also used by the golden-frame tests:

{{< cm-frame ids="large-run-120,large-run-60" >}}

## Which run API?

| | `Invoke-ConsoleRun`, `ConsoleRun.ForEachAsync` | `Start-ConsoleRun`, `LiveRun` |
|---|---|---|
| Who runs the work | ConsoleMonster runs your script block or async body for each item, with throttling, timeouts and retries | Your script or application schedules it; ConsoleMonster only monitors and never changes results |
| Finished items | Stay on one screen as cells; failures are listed when the run ends | Print once into scrollback above a dashboard of bounded height |
| Use it for | Running a script block over many items, one at a time or in parallel | Work already running elsewhere (jobs, worker threads, your own scheduler), or runs too long to keep on one screen |
| Pages | [Live runs](/docs/live-runs/) | [Large runs](/docs/large-runs/) |

## Height, output and controls

The header shows the progress bar, counts, elapsed time, smoothed ETA and throughput. Running rows show names, targets, phases and elapsed time. The next queued items and optional groups use spare height. Warnings and failures stay pinned. In a short terminal, groups disappear first, queued rows collapse to a count, and active rows collapse to the worker count.

At completion, the dashboard is removed and the full summary prints into scrollback: totals, groups, every problem with wrapped detail, and the optional report path. Terminal scrollback retention is controlled by the terminal application.

CI and redirected output use append-only lines. `-NoLive` forces this mode. `-ProgressEvery` and `-ProgressIntervalSeconds` control compact progress updates. C# exposes the same settings through `LiveRunOptions`. `-HeightPercent` changes the dashboard limit.

Background workers can call `$run.Started($index, $phase)` and `$run.Finished($index, $state, $message)` directly. Use `Complete-ConsoleRun $run -Wait` on the calling pipeline to wait and finalize: its output pump keeps host progress moving. A timeout or cancellation leaves unfinished work open. For a non-finalizing checkpoint, wait on the actual PowerShell jobs or call the C# handle's `Wait` method, then join workers before finalization. In C#, observe `run.Cancellation`, let workers report their last outcomes, then call `Complete`.

`-InteractiveKeys` enables `p` to pause/resume completion lines, `f` to filter new log lines to problems, `g` to show/hide groups, and `?` for key help. Work and dashboard counts continue while the log is paused. Held lines flush on resume or completion. `-ShowTrend` adds a throughput sparkline when width permits. Without keyboard input these controls are omitted.

## Next

- [Threads and concurrency](/docs/threads/): report from worker threads and runspaces safely.
- [Live log tails](/docs/live-tails/): follow a file or event log beside a run.
- [Live runs](/docs/live-runs/): let ConsoleMonster run the items for you.
