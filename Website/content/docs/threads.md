---
title: Threads and concurrency
description: "Any thread can update a live view: updates go through one queue, one thread draws, console output is captured, and CI gets whole, ordered lines."
slug: threads
order: 7
group: live
card: threads-live
chips: [Queue, Render thread, Runspaces]
apis: [LiveSession, TerminalScope, Start-ConsoleDashboard -Parallel]
summary: "Any thread can update the screen; one thread draws it."
---

Parallel work and a console do not mix on their own: every thread that writes to the console writes over the others. ConsoleMonster gives the screen to one thread and lets every other thread send it updates, so a live view stays whole however many workers report to it, and output that is not a terminal gets whole lines in order.

{{< cm-demo id="threads-naive" heading="h2" >}}

{{< cm-demo id="threads-live" heading="h2" autoplay="true" >}}

{{< cm-demo id="threads-ci" heading="h2" >}}

## How it works

<figure class="cm-diagram"><svg viewBox="0 0 720 260" role="img" aria-labelledby="cm-threads-title cm-threads-desc"><title id="cm-threads-title">One queue, one thread that draws</title><desc id="cm-threads-desc">Worker threads and runspaces post updates into one first-in first-out queue. The render thread applies them between frames, about 15 times a second, and draws the console. Console.WriteLine calls are captured as whole lines and become updates for the log region.</desc><defs><marker id="cm-threads-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path class="cm-diagram__head" d="M0 0 L10 5 L0 10 z"/></marker></defs><g class="cm-diagram__box"><rect x="10" y="30" width="150" height="34" rx="6"/><rect x="10" y="80" width="150" height="34" rx="6"/><rect x="10" y="130" width="150" height="34" rx="6"/><rect x="270" y="80" width="170" height="34" rx="6"/><rect x="530" y="80" width="180" height="34" rx="6"/><rect x="10" y="210" width="150" height="34" rx="6"/><rect x="270" y="210" width="170" height="34" rx="6"/></g><g class="cm-diagram__text"><text x="85" y="52">worker thread</text><text x="85" y="102">worker thread</text><text x="85" y="152">runspace</text><text x="355" y="102">update queue (FIFO)</text><text x="620" y="102">render thread</text><text x="85" y="232">Console.WriteLine</text><text x="355" y="232">captured line</text></g><g class="cm-diagram__arrow"><path d="M160 47 L270 92" marker-end="url(#cm-threads-arrow)"/><path d="M160 97 L270 97" marker-end="url(#cm-threads-arrow)"/><path d="M160 147 L270 102" marker-end="url(#cm-threads-arrow)"/><path d="M440 97 L530 97" marker-end="url(#cm-threads-arrow)"/><path d="M160 227 L270 227" marker-end="url(#cm-threads-arrow)"/><path d="M355 210 L355 114" marker-end="url(#cm-threads-arrow)"/></g><g class="cm-diagram__note"><text x="215" y="22">post from any thread</text><text x="620" y="62">applies, then draws</text><text x="620" y="140">about 15 frames a second</text><text x="440" y="170">a log entry</text></g></svg></figure>

- **One thread draws.** A live session starts its own render thread. It applies updates to the widgets, composes the frame and writes only the cells that changed. Your work runs on your threads and never touches the screen.
- **Any thread posts.** Handles (`lanes.Handle(session)`, `table.Handle(session)`), `session.Post` and the PowerShell `Update-Console*` commands put an update into one lock-free first-in, first-out queue, and each thread's updates keep their order. The queue has no size limit, so posting does not wait for the screen; a flood that outpaces the render thread for long grows the queue in memory instead.
- **Updates land between frames.** About 15 times a second (`LiveSessionOptions.FramesPerSecond`, 1 to 60) the render thread applies the waiting updates in order, a time-boxed batch per frame (about 40 ms at 15 fps; the rest go into the next one), then draws once. Every update accepted before the session stops is applied by the end; one addressed to a widget name the view does not have is dropped and counted in `EventsDropped`.
- **Console output is captured.** While a session draws on the console, `Console.Out` and `Console.Error` are replaced by writers that collect whole lines from any thread (unless `CaptureConsole = false`; a second capturing session fails to start). With a log region the lines become log entries; an inline area prints them above itself; a full-screen view without a log region prints them after it ends. Write lines, not fragments: two threads calling `Console.Write` without a line break can end up on one line.
- **One write at a time.** Outside a session, `Terminal.Write` and `Terminal.WriteMarkupLine` take one lock, so two threads writing widgets never mix their output. While a session owns the console, each write is queued as a block and printed above an inline area, or after a full-screen view ends.
- **Stopping.** When the work ends or throws, the session stops admitting updates, applies the ones already queued, draws the final frame and restores the terminal before the exception reaches you. The first Ctrl+C does the same for the view and cancels `session.Cancellation`; your work keeps running until it checks that token, and what it posts afterwards is dropped. `HandleCancelKey = false` leaves Ctrl+C to your program.
- **Not a terminal.** Redirected output and CI get stream mode: each change worth a line becomes one whole line, in the order the updates were applied: every lane start and finish, status changes, table rows, finished progress and log entries, with a heartbeat summary every ten seconds. Console output without a log region is printed after the run.

## In PowerShell

The smallest case: eight runspaces at once, each writing with `Write-Host`. The lines go to the log panel, never over the screen:

```powershell
Import-Module ConsoleMonster

$dashboard = New-ConsoleDashboard -Title 'Workers' {
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}
1..8 | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 8 {
    param($Number)
    Start-Sleep -Milliseconds (Get-Random -Maximum 800)
    Write-Host "worker $Number finished"
}
```

`Start-ConsoleDashboard -Parallel` runs a script block per input item in a runspace pool (`-ThrottleLimit`). Each runspace gets `$ConsoleSession`, so `Update-ConsoleLane`, `Update-ConsoleStatus`, `Update-ConsoleTable`, `Write-ConsoleLog` and the other update commands reach the dashboard by widget name. `Write-Host`, warnings and errors from those runspaces (and verbose and debug messages when their preference is on) go to the dashboard's log (a `New-ConsoleLog`; with `-Inline` and no log they print above the area, and a full-screen dashboard without one drops them with a warning); progress records go to its activity list.

```powershell
Import-Module ConsoleMonster

$servers = 'web01', 'web02', 'web03', 'web04'
$dashboard = New-ConsoleDashboard -Title 'Patching' {
    New-ConsolePanel -Title 'Servers' { New-ConsoleStatusList -Name servers -Item $servers }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}

$servers | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 4 {
    param($Server)
    Update-ConsoleStatus -Name servers -Key $Server -Status Running -Detail 'patching'
    Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 900)
    Write-Warning "$Server needs a reboot"
    Update-ConsoleStatus -Name servers -Key $Server -Status Pass -Detail 'patched'
}
```

- Variables of the calling script are not visible inside a runspace, and `$using:` does not work here: pass values with `-ArgumentList` and a `param()` block, or as the input items.
- Change widgets through the update commands, never by calling a widget object's own methods from a runspace.
- `Invoke-ConsoleRun` (also called `ForEach-ConsoleItem`) with `-Parallel` draws its own run view: a summary bar, a cell per item and the failures. Its workers do not get a log panel; what they write is collected and shown after the view ends. Use `Start-ConsoleDashboard -Parallel` when workers should log while they run.

## In C#

Workers update widgets through handles taken from the session. Everything below is safe from any thread while the session runs:

- `LanesHandle`, `StatusListHandle`, `LiveTableHandle`, `ProgressHandle` and the other handles from `widget.Handle(session)`;
- `session.Post`, `session.Write` (a log line) and `session.Region(name).Set(widget)`, which swaps a `Slot`'s content between two frames;
- `Console.WriteLine`, captured as described above;
- the `ConsoleMonster.Extensions.Logging` provider, which writes into the log region of the running session (an entry with an exception is written as ordinary output instead: above an inline area, or after a full-screen view).

The C# tab of the dashboard demo above uses `Parallel.ForEachAsync` with a `LanesHandle`. Code deeper down does not need the session at all: an ambient `TerminalScope` flows with the code it wraps, across `await` and `Task.Run`, and `TerminalScope.Report`, `Advance` and `SetState` go to the widget and key the scope names (`TerminalScope.Log` to the session's log). Outside a scope they do nothing, so the same code runs without a dashboard.

```csharp
using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

string[] servers = { "web01", "web02", "web03" };
Dashboard dashboard = new DashboardBuilder("Patching")
    .Panel("Servers", new Lanes(), out Lanes lanes)
    .BuildDashboard();
foreach (string server in servers) {
    lanes.Add(server, server, 2);
}

using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => Parallel.ForEach(servers, server => {
        // Patch knows nothing about the dashboard: the scope says where its reports go.
        using (s.Scope(lanes.Name, server)) {
            Patch(server);
        }
    }));
}

static void Patch(string server) {
    TerminalScope.Report("copying files");
    Thread.Sleep(300);
    TerminalScope.Advance();
    TerminalScope.Report("restarting");
    Thread.Sleep(300);
    TerminalScope.Advance();
    TerminalScope.SetState("pass");
}
```

`ConsoleRun.ForEachAsync` is the ready-made form: a throttle (`RunOptions.ThrottleLimit`), a per-item timeout the body observes through its token, failures kept per item, and the run view; see [Live runs](/docs/live-runs/).

## Under load

The stress test runs 32 workers posting updates in bursts while four more threads write to `Console.Out` and `Console.Error` at random. It prints its counters when it ends; the recording below is that run. The [benchmarks](/benchmarks/) measure what one frame costs to compose and write.

{{< cm-live id="stress" caption="The stress test: 32 workers and stray console writes, recorded from the program below it." >}}

## What is and is not thread-safe

| Safe from any thread while a session runs | Not safe from several threads |
|---|---|
| Handles from `widget.Handle(session)`, `session.Post`, `session.Write`, `session.Region(name).Set` | Calling a widget object's own methods (`table.Set`, `lanes.Add`, `log.Write`) while the session or another thread uses it |
| `Console.WriteLine` and `Console.Error.WriteLine` (whole lines) | `Console.Write` fragments from several threads, which can join into one line |
| `Terminal.Write` and `Terminal.WriteMarkupLine` | Drawing your own `InlineArea` from several threads |
| `TerminalScope` reports in any task or thread that inherited the scope | |
| The `Update-Console*` commands in `Start-ConsoleDashboard -Parallel` runspaces | Widget objects changed from a runspace instead of through the update commands |

Build a widget fully before the session starts, then change it only through handles or update commands while it runs. A handle used after the session stopped is harmless: the update is dropped and counted in `EventsDropped`.
