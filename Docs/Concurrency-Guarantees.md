# Concurrent work and output

Workers send updates through a live session's dispatcher. The session applies them serially and draws frames from that state.
PowerShell's parallel commands use the same dispatcher on Windows PowerShell 5.1 and PowerShell 7.

```powershell
Import-Module ConsoleMonster
'alpha', 'beta', 'gamma' | ForEach-ConsoleItem -Parallel -ThrottleLimit 3 {
    Write-Host "Checking $_"
    Write-Progress -Id 0 -Activity "Checking $_" -PercentComplete 50
    if ($_ -eq 'beta') { Write-Warning 'A retry was needed' }
    "$_ checked"
}
```

In an interactive console, parallel runs show messages in a log and progress in an activity list.
Each invocation owns its progress IDs and parent/child relationships, so workers can reuse `-Id 0` safely.
Completing a parent removes its children. Ending an invocation clears its remaining activities, as PowerShell does.
Ordinary results keep input order. Native warning/error/information records are delivered after the live view ends,
preserving `-WarningVariable` and downstream records; the live log contains one copy of each captured message.
Without an interactive console, run commands print finished-item lines and a summary while results travel through the pipeline.

`Start-ConsoleDashboard -Parallel` also captures host, warning, information, verbose/debug, error, and progress streams from
its owned runspaces. Dashboard scripts receive caller values with `-ArgumentList` and `param()`; the run commands use `$using:name`.

## Output and queue behavior

`Console.WriteLine` and captured PowerShell host/log messages share log routing. With no explicit log region:

- Full screen shows an implicit log tail using at most six rows and one third of the frame. Captured lines also print to
  scrollback when the full screen ends; the original content keeps the remaining space.
- Inline mode writes captured lines above its live area as frames advance.
- Stream/CI mode writes captured lines progressively, in apply order, while work is still running.

An explicit log region retains its configured capacity, 1,000 lines by default. Deliberately evicted history does not count as
dropped dispatcher updates. The finite stress example retains its captured dataset to verify every delivered identity.

One `Terminal.Write` or `Terminal.WriteLines` call is a complete block. Concurrent calls sharing an output cannot interleave its
lines. During a session, the prepared block has one queue admission decision. If shutdown wins before admission, the caller
writes the whole block once, serialized with the session's remaining output and restoration. Stream fallback uses the session's
output before restoration; late blocks during final cleanup wait for its final flush. Normal output resumes after cleanup.
Accepted blocks appear above an inline area or after a full screen ends.

`Console.Write` fragments and CR/LF state are buffered per managed thread until newline. Stopping flushes each unfinished tail
once. A logical message that continues on a different thread should use one `WriteLine` or `Terminal.Write` call.

Posting does not wait for the renderer or queue capacity. The queue is unbounded: memory grows with a blocked renderer's backlog.
Runtime allocation, GC, and scheduling still apply; this is not a real-time latency guarantee. The flood test holds `Apply`
blocked while 32 producers enqueue 131,072 updates, records the retained managed-heap change, then verifies every accepted event
drains once. The memory observation is diagnostic, not a fixed CI budget. Full-screen output retained for final scrollback also
consumes memory until shutdown.

Each producer's order is preserved. Concurrent producers have no prescribed order relative to one another; line output follows
actual apply order. There is no latest-wins coalescing: every accepted event retains its normal ordering and counters.
Many applied updates can share one rendered frame without dropping updates.

The first Ctrl+C closes admission and requests graceful cancellation. Later posts are rejected and counted in `EventsDropped`;
accepted work drains during shutdown. Graceful shutdown restores console writers, native Windows output mode, and cursor visibility.
Native VT processing stays enabled through final scrollback and summary output, then the original output mode is restored.
A callback that prevents the render thread from joining can exceed `StopTimeout`. Console capture is restored, while the final
drain and screen cleanup are deferred until that callback returns. Workers should observe cancellation. Use `GetCounters()` to inspect accepted, applied,
dropped, and failed updates. Zero drops is the normal-run contract; a late post after cancellation is an expected rejection.

## .NET alternative

Create widgets on the definition thread, then use handles from workers. Direct widget mutations such as `Slot.Set`,
`LogRegion.Write`, and property changes remain setup operations, not thread-safe live updates.

```csharp
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using System.Threading.Tasks;

var summary = new SummaryBar("summary");
var slot = new Slot("main", TextBlock.Plain("Waiting"));
var log = new LogRegion();
using (var session = new LiveSession(new Rows().Add(summary).Add(slot).Add(log))) {
    session.Run(s => {
        SummaryBarHandle totals = summary.Handle(s);
        Parallel.For(0, 8, worker => {
            totals.SetCounter("Worker " + worker, 1, StatusKind.Pass);
            log.Handle(s).Write("Worker " + worker + " finished");
            s.Region("main").Set(TextBlock.Plain("Latest worker: " + worker));
        });
        totals.SetProgress(done: 8, total: 8);
    });
}
```

`SummaryBarHandle.SetProgress` can update either value independently; omitted values retain the preceding queued value.
`SetCounter` replaces a counter by label in apply order. `session.Region(name).Set(widget)` and `Slot.Handle(session).Set(widget)`
enqueue swaps. A swap invalidates layout and forces the next frame to redraw fully.
Normal application happens on the render thread; after that thread joins, the stopping caller can perform the final drain.
These phases remain serialized. Read mutable widget state after stopping or from the applying thread.

## Guarantees and their tests

These tests assert identities, output, ordering, and synchronization. Watchdogs bound a deadlock, not throughput.
Core tests run on net472, net8.0 and net10.0; PowerShell tests run on 5.1 and 7.

| Requirement | Evidence |
| --- | --- |
| Complete concurrent one-shot blocks | [`TerminalWrite_ManyProducersKeepEveryMultilineBlockWhole`](../ConsoleMonster.Tests/Live/TerminalBlockConcurrencyTests.cs) |
| One admission per block and preserved final inline line | `SessionWrites_AdmitWholeMultilineBlocksBeforeAnotherProducer`, `SessionWrite_PreservesTheFinalInlineLine` in the same file |
| Exactly-once shutdown fallback to actual OS stdout | [`StopDuringWritePreparation_PrintsTheWholeFallbackExactlyOnceToNativeStdout`](../ConsoleMonster.Tests/Live/SessionProcessTests.cs) |
| Shutdown fallback cannot split draining blocks; restoration/summary writes flush once | [`ShutdownFallback_KeepsItsBlockWholeWhileAnAcceptedBlockIsStillDraining`, `ShutdownFallback_FromRestorationAndSummaryIsFlushedExactlyOnce`](../ConsoleMonster.Tests/Live/TerminalBlockConcurrencyTests.cs) |
| Default output from a frame/summary callback cannot deadlock with a concurrent Snapshot request | [`RenderCallback_DefaultOutputAndConcurrentSnapshotCannotDeadlock`](../ConsoleMonster.Tests/Live/SessionProcessTests.cs), forced frame and summary schedules in isolated processes |
| Whole stream lines in apply order, many producers | [`Stream_ManyProducersWriteWholeLinesInExactApplyOrder`](../ConsoleMonster.Tests/Live/DispatcherConcurrencyTests.cs) |
| Posting proceeds with blocked Apply; every accepted flood event drains | `Flood_PostingFinishesWhileApplyIsBlocked_ThenEveryAcceptedUpdateDrains` in the same file |
| Producer order and serialized normal application under stress | [`ConcurrentProducersPreserveEveryEventAndProducerOrderOnTheRenderThread`](../ConsoleMonster.Tests/Live/LiveSessionTests.cs) |
| ThrottleLimit bounds ForEachAsync bodies | [`ForEachAsync_ThrottleBoundsActiveBodiesUnderPressure`](../ConsoleMonster.Tests/Live/ThrottleConcurrencyTests.cs), limits 1, 3, and 8 |
| Concurrent handles, SummaryBar partial updates, and queued Slot swaps | [`HandlesAndSlotSet_KeepEveryUpdateAndEachProducersLastValue`](../ConsoleMonster.Tests/Live/HandleConcurrencyTests.cs) |
| Per-thread fragments, CR/LF, and exactly-once tail flushing | [`ConcurrentFragments_StayWithTheirProducerUntilNewline`, `CarriageReturnState_BelongsToTheProducer`, `Complete_FlushesEveryProducerTailExactlyOnce`](../ConsoleMonster.Tests/Live/CaptureWriterConcurrencyTests.cs) |
| Progressive no-log output and bounded full-screen tail with scrollback | [`StreamWithoutLog_WritesCapturedAndPostedLinesBeforeStopInApplyOrder`, `FullScreenWithoutLog_ShowsABoundedCaptureTailAndRetainsScrollback`](../ConsoleMonster.Tests/Live/UnclaimedOutputTests.cs) |
| LF/CRLF host, warning, and colored messages retain lines without a log | `NoLog_MultilinePlainAndColouredMessagesKeepTheirLines` in the same file; [`preserves multiline host and warning messages without a log in <Mode> with <LineEnding>`](../Module/Tests/ConsoleMonster.Concurrency.Tests.ps1) |
| Concurrent log entries retain exception details and scopes | [`ConcurrentExceptionEntries_KeepMessagesDetailsAndScopesTogether`](../ConsoleMonster.Extensions.Tests/LoggingConcurrencyTests.cs) |
| All session log entries arrive; exceptions reach the configured explicit sink | `ConcurrentSessionLogging_DeliversEveryEntryAndAnExceptionToItsExplicitSink` in the same file |
| Parallel dashboard/run host, warning, nested progress, and native results | [`routes host, warning and nested progress from <CommandName> without changing result records`](../Module/Tests/ConsoleMonster.Concurrency.Tests.ps1), all three command names |
| First actual Ctrl+C rejects later posts and restores terminal/writers | [`FirstActualCtrlC_RejectsLaterPostsAndRestoresNativeTerminalAndWriters`](../ConsoleMonster.Tests/Live/SessionProcessTests.cs), isolated Windows console; [`FirstCancellation_DropsLaterPostsAndRestoresWritersExactlyOnce`](../ConsoleMonster.Tests/Live/CancellationConcurrencyTests.cs), portable cancellation |
| Stress proves zero drops and actual captured line identities | [`StressRun_VerifiesZeroDropsAndCapturedLineIdentitiesAfterDrain`](../ConsoleMonster.Tests/Live/StressRunTests.cs) and the real stress example's delivered-line assertions |
| Native Format-Wide fitting | [`matches native Wide column fitting for <LayoutName>, including boundary widths and groups`](../Module/Tests/ConsoleMonster.FormatCommands.Tests.ps1); direct-host proof in [`Assert-ConcurrencyHost.ps1`](../Module/Tests/Support/Assert-ConcurrencyHost.ps1) |
