---
title: Live sessions
description: A live session redraws about 15 times a second while any thread posts updates, captures console output and handles resizes and small windows.
slug: live-sessions
order: 6
group: live
card: size-100x30
apis: [LiveSession, MinimumSize]
summary: "Dashboards updated from any thread, inline areas and windows that are too small."
---

`LiveSession` owns the screen while work runs. Updates are `LiveEvent`s addressed to widgets by name; they go into a lock-free queue and are applied on the render thread, which composes the tree, diffs it against the previous frame and writes only the cells that changed, about 15 times a second.

{{< cm-demo id="session" heading="h2" >}}

The [live dashboards page](/live/) shows larger programs built the same way, recorded under load.

## What a session takes care of

- **Threads.** Handles (`lanes.Handle(session)`) post events from any thread. An ambient `TerminalScope` lets deep code report status without passing the session around.
- **Console output.** `Console.Out` and `Console.Error` are captured into the log region while the session runs (`CaptureConsole`, on by default). In PowerShell, `Start-ConsoleDashboard` runs the script in a runspace whose `Write-Host`, warnings, errors and verbose output land there too.
- **The end.** The screen is restored, also on Ctrl+C and exceptions, and the final state (or `LiveSessionOptions.Summary`) is printed to the scrollback.
- **Problems.** Updates for unknown names, failed updates and frames that could not be laid out are counted (`GetCounters()`) and reported once the screen is restored.

## Inline areas and Write-Progress

`LiveMode.Inline` (`Start-ConsoleDashboard -Inline`) draws an area as tall as its content below the cursor; output scrolls above it and the final state stays in the scrollback, or nothing with `-Transient`. `Write-Progress` in a dashboard script shows as nested activities, and `Use-ConsoleProgress` renders an existing script's `Write-Progress` inline without changing it.

## Progress columns

`Lanes` and `ProgressBar` take a column model. Without `Columns` they keep the classic layout: label, bar, percentage, pass, fail and warning counts, and the detail.

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

Lanes lanes = new Lanes();
lanes.Columns = ProgressColumns.Standard();                     // spinner, description, bar, %, count, elapsed, ETA, status
lanes.Columns.Insert(7, ProgressColumns.Rate(unit: "files"));   // "12 files/s"; Rate(bytes: true) shows "1.5 MB/s"
lanes.Columns.Add(ProgressColumns.Custom("Phase", row => row.IsFinished ? "verified" : "copying"));
lanes.HideCompleted = true;                                     // finished lanes leave; the summary line counts them
lanes.Add("dc01", "DC01", 40);
lanes.Add("dc02", "DC02", 40);
Terminal.Write(lanes);
```
```powershell
Import-Module ConsoleMonster

New-ConsoleLanes -Name servers -Lane 'DC01', 'DC02' -Total 40 -Column Standard, Rate -HideCompleted | Write-ConsoleWidget
New-ConsoleProgress 'Upload' -Total 500MB -Column Description, Bar, Percent, ByteRate, Remaining | Write-ConsoleWidget
```

Rate and remaining time are smoothed over a moving window (10 seconds by default), so a stalled task's rate falls instead of freezing. When the region is too narrow, the lowest-priority columns go first, and in stream mode the heartbeat line summarises the running lanes through the same columns.

## Replaceable regions and live tables

A `Slot` is a named region whose widget is replaced while the session runs; the swap goes through the update queue, so it is thread-safe and lands between frames. `LiveTable` is a table whose rows have stable keys and are added, updated, removed or cleared; its column widths only grow, `MaxRows` keeps the most recent rows and `SortColumn` keeps them sorted.

```powershell
Import-Module ConsoleMonster

$controllers = @(
    [pscustomobject] @{ Name = 'DC01'; Site = 'Warsaw'; Status = 'Pass' }
    [pscustomobject] @{ Name = 'DC02'; Site = 'Dublin'; Status = 'Warn' }
)
$d = New-ConsoleDashboard -Title 'Domain health' {
    New-ConsolePanel -Title 'Domain controllers' { New-ConsoleRegion -Name main { New-ConsoleSpinner -Label 'Querying' } }
    New-ConsolePanel -Title 'Jobs' -Fill { New-ConsoleLiveTable -Name jobs -Column 'Job', 'State', 'Took' -MaxRows 8 }
}
Start-ConsoleDashboard $d -ArgumentList (, $controllers) {
    param($Controllers)
    Update-ConsoleTable -Name jobs -Key 'dns@DC01' -Value 'dns', 'Running', ([timespan]::Zero)
    Start-Sleep -Seconds 1
    Update-ConsoleTable -Name jobs -Key 'dns@DC01' -Value 'dns', 'Done', ([timespan]::FromSeconds(1))
    Set-ConsoleRegion -Name main { $Controllers | Format-ConsoleTable -AsWidget }
    Start-Sleep -Seconds 1
}
```

A recording keeps the text form of row values; region swaps carry in-process widgets and are not replayed.

## Resizing and small windows

A size change clears the screen and redraws it; while a window is dragged, redraws are coalesced to at most one every 100 ms. Narrower panels drop optional parts first, table columns hide by priority and tables turn into cards. When the window is smaller than the dashboard's `MinimumSize`, it keeps its highest-priority panels, and when even those do not fit, it shows one status line.

{{< cm-frame ids="size-100x30,size-64x20,size-50x12" caption="The replay program from the live dashboards page (`dotnet run -- assessment-run.jsonl 2`, with the recording offered there) halfway through, in windows of 100×30, 64×20 and 50×12. Its `MinimumSize` is 60×20." >}}

## Without a console

In CI, with redirected output and in stream mode, a session narrates append-only lines instead of redrawing, with `[mm:ss]` prefixes when a CI system is detected. Plain mode does the same in ASCII without colour, and text that reaches stream and plain output is stripped of escape sequences and control characters.