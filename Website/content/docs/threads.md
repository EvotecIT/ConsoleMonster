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

<figure class="cm-diagram" aria-label="Worker threads post updates to one queue; the render thread applies them between frames and draws; console output is captured into the log region"><svg viewBox="0 0 720 250" role="img"><title>One queue, one thread that draws</title><defs><marker id="cm-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path class="cm-diagram__head" d="M0 0 L10 5 L0 10 z"/></marker></defs><g class="cm-diagram__box"><rect x="10" y="20" width="150" height="34" rx="6"/><rect x="10" y="64" width="150" height="34" rx="6"/><rect x="10" y="108" width="150" height="34" rx="6"/><rect x="250" y="64" width="170" height="34" rx="6"/><rect x="510" y="64" width="200" height="34" rx="6"/><rect x="10" y="190" width="150" height="34" rx="6"/><rect x="250" y="190" width="170" height="34" rx="6"/></g><g class="cm-diagram__text"><text x="85" y="42">worker thread</text><text x="85" y="86">worker thread</text><text x="85" y="130">runspace</text><text x="335" y="86">update queue (FIFO)</text><text x="610" y="86">render thread → console</text><text x="85" y="212">Console.WriteLine</text><text x="335" y="212">captured line</text></g><g class="cm-diagram__arrow"><path d="M160 37 L250 76"/><path d="M160 81 L250 81"/><path d="M160 125 L250 86"/><path d="M420 81 L510 81"/><path d="M160 207 L250 207"/><path d="M335 190 L335 98"/></g><g class="cm-diagram__note"><text x="205" y="60">post</text><text x="465" y="70">about 15 frames a second</text><text x="380" y="150">into the log region</text></g></svg></figure>

- **One thread draws.** A live session starts its own render thread. It applies updates to the widgets, composes the frame and writes only the cells that changed. Your work runs on your threads and never touches the screen.
- **Any thread posts.** Handles (`lanes.Handle(session)`, `table.Handle(session)`), `session.Post` and the PowerShell `Update-Console*` commands put an update into one lock-free queue. The queue has no size limit, so posting never blocks; each thread's updates keep their order.
- **Updates land between frames.** About 15 times a second (`LiveSessionOptions.FramesPerSecond`, 1 to 60) the render thread applies everything waiting in the queue, in order, then draws once. Every update is applied, and all the updates that arrive between two frames are drawn by one frame.
- **Console output is captured.** While a session runs, `Console.Out` and `Console.Error` are replaced by writers that collect whole lines from any thread. With a log region they become log entries; an inline area prints them above itself; a full-screen view without a log region prints them after it ends. Write lines, not fragments: two threads calling `Console.Write` without a line break can end up on one line.
- **One write at a time.** Outside a session, `Terminal.Write` and `Terminal.WriteMarkupLine` take one lock, so two threads writing widgets never mix their output. While a session owns the console, these writes join its output line by line.
- **Stopping.** When the work ends or throws, the session stops admitting updates, applies the ones already queued, draws the final frame and restores the terminal before the exception reaches you. The first Ctrl+C cancels `session.Cancellation` (your work decides how to stop on it); a second one restores the terminal and ends the process.
- **Not a terminal.** Redirected output and CI get stream mode: each update becomes one whole line, written in the order the updates were applied, with a heartbeat summary every ten seconds.

## In PowerShell

`Start-ConsoleDashboard -Parallel` runs a script block per input item in a runspace pool (`-ThrottleLimit`). Each runspace gets `$ConsoleSession`, so `Update-ConsoleLane`, `Update-ConsoleStatus`, `Update-ConsoleTable`, `Write-ConsoleLog` and the other update commands reach the dashboard by widget name. `Write-Host`, warnings, verbose and debug messages, errors and progress from those runspaces go to the log panel.

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

- Pass what a runspace needs as its input item or parameters: variables of the calling script are not visible inside it.
- Change widgets through the update commands, never by calling a widget object's own methods from a runspace.
- `ForEach-ConsoleItem -Parallel` and `Invoke-ConsoleRun` draw their own run view (a summary bar, a cell per item, the failures). Their workers do not get the log panel: what they write is collected and shown after the view ends. Use `Start-ConsoleDashboard -Parallel` when workers should log while they run.

## In C#

Workers update widgets through handles taken from the session. Everything below is safe from any thread while the session runs:

- `LanesHandle`, `StatusListHandle`, `LiveTableHandle`, `ProgressHandle` and the other handles from `widget.Handle(session)`;
- `session.Post`, `session.Write` (a log line) and `session.Region(name).Set(widget)`, which swaps a `Slot`'s content between two frames;
- `Console.WriteLine`, captured as described above;
- the `ConsoleMonster.Extensions.Logging` provider, which writes into the log region while a session shows it (an entry with an exception is written as ordinary output instead).

The C# tab of the dashboard demo above uses `Parallel.ForEachAsync` with a `LanesHandle`. Code deeper down does not need the session at all: an ambient `TerminalScope` flows with the code it wraps, across `await` and `Task.Run`, and `TerminalScope.Report`, `Advance`, `SetState` and `Log` go to the widget and key the scope names.

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
}
```

`ConsoleRun.ForEachAsync` is the ready-made form: a throttle (`RunOptions.ThrottleLimit`), a per-item timeout the body observes through its token, failures kept per item, and the run view; see [Live runs](/docs/live-runs/).

## Under load

The stress test runs 32 workers posting as fast as they can while four more threads write to `Console.Out` and `Console.Error` at random. It prints its counters when it ends; the recording below is that run. The [benchmarks](/benchmarks/) measure what one frame costs to compose and write.

{{< cm-live id="stress" caption="The stress test: 32 workers and stray console writes, recorded from the program below it." >}}

## What is and is not thread-safe

| Safe from any thread while a session runs | Not safe from several threads |
|---|---|
| Handles from `widget.Handle(session)`, `session.Post`, `session.Write`, `session.Region(name).Set` | Calling a widget object's own methods (`table.Set`, `lanes.Add`, `log.Write`) while the session or another thread uses it |
| `Console.WriteLine` and `Console.Error.WriteLine` (whole lines) | `Console.Write` fragments from several threads, which can join into one line |
| `Terminal.Write` and `Terminal.WriteMarkupLine` | Drawing your own `InlineArea` from several threads |
| `TerminalScope` reports in any task or thread that inherited the scope | A handle used after the session stopped (the update is counted in `EventsDropped`) |
| The `Update-Console*` commands in `Start-ConsoleDashboard -Parallel` runspaces | Widget objects changed from a runspace instead of through the update commands |

Build a widget fully before the session starts, then change it only through handles or update commands while it runs.
