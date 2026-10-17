---
title: Live sessions
description: A live session redraws about 15 times a second while any thread posts updates, captures console output and handles resizes and small windows.
slug: live-sessions
order: 5
---

`LiveSession` owns the screen while work runs. Updates are `LiveEvent`s addressed to widgets by name; they go into a lock-free queue and are applied on the render thread, which composes the tree, diffs it against the previous frame and writes only the cells that changed, about 15 times a second.

```csharp
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Dashboard dashboard = new DashboardBuilder("Deploy")
    .Panel("Servers", new Lanes(), out Lanes lanes, caption: "4 workers")
    .Panel("Log", new LogRegion(), out LogRegion log, fill: true)
    .BuildDashboard();

using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => {
        LanesHandle servers = lanes.Handle(s);           // thread-safe from any thread
        Parallel.ForEach(new[] { "SRV1", "SRV2", "SRV3" }, server => {
            servers.Start(server);
            servers.Advance(server, StatusKind.Pass, "copying files");
            Console.WriteLine(server + " done");          // captured into the log region
            servers.Complete(server);
        });
    });
}
```

This is the README's example. The [live dashboards page](/live/) replays the showcases that use the same API under load.

## What a session takes care of

- **Threads.** Handles (`lanes.Handle(session)`) post events from any thread. An ambient `TerminalScope` lets deep code report status without passing the session around.
- **Console output.** `Console.Out` and `Console.Error` are captured into the log region while the session runs (`CaptureConsole`, on by default). In PowerShell, `Start-ConsoleDashboard` runs the script in a runspace whose `Write-Host`, warnings, errors and verbose output land there too.
- **The end.** The screen is restored, also on Ctrl+C and exceptions, and the final state (or `LiveSessionOptions.Summary`) is printed to the scrollback.
- **Problems.** Updates for unknown names, failed updates and frames that could not be laid out are counted (`GetCounters()`) and reported once the screen is restored.

## Inline areas and Write-Progress

`LiveMode.Inline` (`Start-ConsoleDashboard -Inline`) draws an area as tall as its content below the cursor; output scrolls above it and the final state stays in the scrollback, or nothing with `-Transient`. `Write-Progress` in a dashboard script shows as nested activities, and `Use-ConsoleProgress` renders an existing script's `Write-Progress` inline without changing it.

## Resizing and small windows

A size change clears the screen and redraws it; while a window is dragged, redraws are coalesced to at most one every 100 ms. Narrower panels drop optional parts first, table columns hide by priority and tables turn into cards. When the window is smaller than the dashboard's `MinimumSize`, it keeps its highest-priority panels, and when even those do not fit, it shows one status line.

{{< cm-frame ids="size-100x30,size-64x20,size-50x12" caption="The assessment dashboard halfway through its recording, composed at 100×30, 64×20 and 50×12. Its MinimumSize is 60×20." >}}

## Without a console

In CI, with redirected output and in stream mode, a session narrates append-only lines instead of redrawing, with `[mm:ss]` prefixes when a CI system is detected. Plain mode does the same in ASCII without colour, and text that reaches stream and plain output is stripped of escape sequences and control characters.