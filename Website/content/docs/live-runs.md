---
title: Live runs
description: Run a script block for many items in parallel with a live view, re-run a command with Watch-Console, and watch Pester tests as they run.
social_card_image: /site/cards/size-100x30.svg
meta.social_title: "Live runs: parallel, watch and Pester"
slug: live-runs
order: 9
group: live
card: parallel
apis: [ForEach-ConsoleItem, Watch-Console]
chips: [Pester]
summary: "ForEach-ConsoleItem in parallel, Watch-Console and Pester runs, recorded."
---

Three commands turn long-running work into a live view on an interactive console and into plain lines everywhere else: `ForEach-ConsoleItem` (`Invoke-ConsoleRun`) for many items, `Watch-Console` for a command you want to see change, and `Invoke-ConsolePester` for tests. Each writes ordinary PowerShell output when it ends, so it fits into a script like the commands it replaces.

{{< cm-demo id="parallel" heading="h2" autoplay="true" >}}

## What the run view shows

- A summary bar: done of total, the percentage, the time left (estimated from the time so far), and counters for passed, failed, skipped and running items. Counters keep their order, and zeros are dimmed rather than hidden, so the line does not jump.
- A cell map: one symbol per item in its status colour, so a run of hundreds of items still fits on a few rows.
- The items running now, with how long each has been running.
- The failures, listed with their messages when the run ends. An item that runs past `-TimeoutSeconds` is stopped and counted as failed.

Without `-Parallel` the items run one at a time in the caller's scope, as with `ForEach-Object`. With it, up to `-ThrottleLimit` items run at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7; pass the caller's variables with `$using:name`. In redirected output, CI and jobs, the view becomes one line per finished item and a summary.

The summary bar and the cell map are widgets of their own, for your own views:

```powershell
$states = @('Pass') * 28 + @('Fail') * 2 + @('Warn') + @('Running') * 4
New-ConsoleRows -Gap 1 {
    New-ConsoleSummaryBar -Done 31 -Total 40 -Elapsed 42s -Counter ([ordered] @{ passed = 28; failed = 2; warnings = 1 })
    New-ConsoleCellMap -Status $states -Count 40
} | Write-ConsoleWidget
```

In C#, `ConsoleRun.ForEachAsync` runs the same view around any async work, and `SummaryBar` and `CellMap` are in `ConsoleMonster.Widgets`.

{{< cm-demo id="watch" heading="h2" >}}

`Watch-Console` runs the script block every `-Interval` seconds and replaces the view with its latest output, shaped as `Format-ConsoleTable` shapes it. `-Differences` marks the cells that changed since the previous run, a run that fails shows its error and the watch goes on, and `-Count` ends it after that many runs. Ctrl+C ends it and leaves the last frame in the scrollback. Piped on, or without an interactive console, every run is printed instead.

{{< cm-demo id="pester" heading="h2" >}}

`Invoke-ConsolePester` takes Pester's own parameters (`-Path`, `-TagFilter`, `-Configuration`...) and shows the run as it happens: a file list with progress, one cell per test, the test running now and each failure with its expected and actual values. It returns Pester's result object with `-PassThru`. When the run ends, a table of results by file, the failures and the totals are written; in redirected output and CI they are plain text.