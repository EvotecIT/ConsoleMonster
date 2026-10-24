using System;
using ConsoleMonster.Glyphs;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LargeRunLifecycleTests {
        [Theory] [InlineData(false)] [InlineData(true)]
        public void PlainSinkKeepsWordsAndAllCompletionDetailsWhileRecordingAndReplayKeepSeverity(bool noLive) {
            var profile = TerminalProfile.Fixed(120, 24, ColorDepth.TrueColor);
            var plain = profile.WithMode(OutputMode.Plain);
            var output = new StringTerminalWriter();
            var recording = new System.IO.StringWriter();
            StatusKind[] states = { StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip };
            string[] details = { "Healthy endpoint", "Connection refused", "Partial response", "Unsupported on this platform" };
            using (var run = new LiveRun(states.Select((state, i) => new RunItem("result-" + i)), new LiveRunOptions {
                NoLive = noLive, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new LineOutput(output, plain), RecordWriter = recording,
                    CaptureConsole = false, HandleCancelKey = false, Discoverable = false }
            })) {
                for (int i = 0; i < states.Length; i++) { run.Finished(i, states[i], details[i]); }
                run.Complete();
                Assert.Equal(new[] { "pass", "err", "warn", "skip" }, run.Dashboard.Log.Entries.Where(entry => entry.Text.StartsWith("result-", StringComparison.Ordinal)).Select(entry => entry.Level));
            }
            foreach (string word in new[] { "OK", "ERR", "WARN", "SKIP" }) { Assert.Contains(word, output.ToString()); }
            foreach (string detail in details) { Assert.Contains(detail, output.ToString()); }
            Assert.True(!output.ToString().Contains("\u001b"), output.ToString().Replace("\u001b", "<ESC>"));
            LiveEvent[] events = LiveRecordingReader.Read(new System.IO.StringReader(recording.ToString()))
                .Where(item => item.Text?.StartsWith("result-", StringComparison.Ordinal) == true).ToArray();
            Assert.Equal(new[] { "pass", "error", "warn", "skip" }, events.Select(item => item.State));
            Assert.All(events, item => Assert.Null(item.Payload));
            var replay = new LogRegion("run-log") { ShowTime = false };
            var replayOutput = new StringTerminalWriter();
            LiveSession.Replay(replay, new System.IO.StringReader(recording.ToString()), new LiveSessionOptions {
                Profile = plain, Output = new LineOutput(replayOutput, plain), CaptureConsole = false, HandleCancelKey = false, Discoverable = false
            }, speed: 0);
            Assert.Equal(new[] { "pass", "err", "warn", "skip" }, replay.Entries.Where(entry => entry.Text.StartsWith("result-", StringComparison.Ordinal)).Select(entry => entry.Level));
            foreach (string detail in details) { Assert.Contains(detail, replayOutput.ToString()); }
        }

        [Fact]
        public async System.Threading.Tasks.Task FlushDoesNotHoldTheUpdateGateWhileCallingAnOutputSink() {
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None);
            var writer = new CallbackWriter();
            using (var entered = new System.Threading.ManualResetEventSlim())
            using (var proceed = new System.Threading.ManualResetEventSlim())
            using (var run = new LiveRun(new[] { new RunItem("first"), new RunItem("from writer") }, new LiveRunOptions {
                ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new InlineOutput(writer, profile), CaptureConsole = false, HandleCancelKey = false,
                    WindowSource = () => new[] { 0, 0, 80, 24 } }
            })) {
                writer.Callback = text => {
                    if (!text.Contains("OK") || !text.Contains("first")) { return; }
                    writer.Callback = null; entered.Set();
                    if (!proceed.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("The flush was not published."); }
                    run.Finished(1, StatusKind.Pass);
                };
                try {
                    run.Finished(0, StatusKind.Pass);
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
                    var flush = System.Threading.Tasks.Task.Run(() => run.Flush(TimeSpan.FromSeconds(5)));
                    Assert.True(System.Threading.SpinWait.SpinUntil(() => run.Session.EventsPosted >= 2, 5000));
                    proceed.Set(); Assert.True(await flush);
                    Assert.Equal(2, run.Snapshot().Passed);
                } finally { proceed.Set(); }
            }
        }

        private sealed class CallbackWriter : ITerminalWriter {
            internal Action<string>? Callback;
            internal Action? Flushing;
            public bool IsConsole => false;
            public void Write(string text) => Callback?.Invoke(text);
            public void Flush() => Flushing?.Invoke();
            public void Dispose() { }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompletionReportsFinalSummaryAndFlushFailuresWhileKeepingResults(bool flushFailure) {
            var failure = new System.IO.IOException("Final output failed");
            var writer = new CallbackWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            var run = new LiveRun(new[] { new RunItem("completed item", "node-01") }, new LiveRunOptions {
                ProgressEvery = 0, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new LineOutput(writer, profile), CaptureConsole = false, HandleCancelKey = false }
            });
            try {
                run.Finished(0, StatusKind.Pass, "worker result", TimeSpan.FromSeconds(2));
                Assert.True(run.Flush(TimeSpan.FromSeconds(5))); Assert.Null(run.OutputFailure);
                if (flushFailure) { writer.Flushing = () => throw failure; }
                else { writer.Callback = text => { if (text.Contains("1 / 1 finished")) { throw failure; } }; }

                var error = Assert.Throws<InvalidOperationException>(() => run.Complete());

                Assert.Same(failure, error.InnerException); Assert.Same(failure, run.OutputFailure);
                Assert.False(run.Session.IsRunning);
                RunSnapshot snapshot = run.Snapshot();
                Assert.True(snapshot.IsFinished); Assert.Equal(1, snapshot.Passed);
                Assert.Equal("worker result", snapshot.Items[0].Message);
                Assert.Equal(TimeSpan.FromSeconds(2), snapshot.Items[0].Duration);
            } finally { run.Dispose(); }
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        public void EffectiveOutputModeControlsProgressAndInteractiveKeys(bool explicitProfile, bool noLive, bool borrowedPlain) {
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor);
            var writer = new System.IO.StringWriter();
            var target = new WriterTarget(writer, profile);
            using (Terminal.UseProfilePolicy(p => p.WithMode(noLive || borrowedPlain ? OutputMode.Interactive : OutputMode.Plain)))
            using (var run = new LiveRun(new[] { new RunItem("check", "node-01") }, new LiveRunOptions {
                Title = "Policy run", NoLive = noLive, InteractiveKeys = true, ProgressEvery = 1, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions {
                    Profile = explicitProfile ? profile : null, Target = target,
                    Output = borrowedPlain ? new LineOutput(target.Writer!, profile.WithMode(OutputMode.Plain)) : null,
                    CaptureConsole = false, HandleCancelKey = false
                }
            })) {
                Assert.False(run.IsLive); Assert.False(run.HandleKey('p')); Assert.False(run.HandleKey('f')); Assert.False(run.HandleKey('g'));
                run.Finished(0, StatusKind.Pass); run.Complete();
                string text = writer.ToString();
                Assert.Contains("Policy run", text); Assert.Contains("progress 1/1", text);
                Assert.DoesNotContain("\u001b[?25", text); Assert.DoesNotContain("\u001b[?2026", text);
                if (!noLive || borrowedPlain) { Assert.True(!text.Contains("\u001b"), text.Replace("\u001b", "<ESC>")); Assert.Contains("OK", text); }
            }
        }

        [Fact]
        public void CompletionScrollbackKeepsStateGlyphsWithoutRepeatingStatusWords() {
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor, GlyphTier.Basic);
            var writer = new StringTerminalWriter();
            using (var run = new LiveRun(Enumerable.Range(0, 4).Select(i => new RunItem("work " + i)), new LiveRunOptions {
                ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new InlineOutput(writer, profile), CaptureConsole = false, HandleCancelKey = false,
                    WindowSource = () => new[] { 0, 0, 80, 24 } }
            })) {
                run.Finished(0, StatusKind.Pass); run.Finished(1, StatusKind.Fail); run.Finished(2, StatusKind.Warn); run.Finished(3, StatusKind.Skip);
                Assert.True(run.Flush(TimeSpan.FromSeconds(5)));
                string plain = Regex.Replace(writer.ToString(), "\u001b\\[[0-9;?]*[ -/]*[@-~]", "");
                var context = Terminal.CreateContext(profile);
                foreach (StatusKind state in new[] { StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip }) { Assert.Contains(context.Glyphs.StatusSymbol(state), plain); }
                Assert.DoesNotContain("OK work", plain); Assert.DoesNotContain("ERR work", plain); Assert.DoesNotContain("WARN work", plain);
                Assert.Equal(0, run.Session.EventsDropped);
            }
        }

        [Fact]
        public void OutputFailureCannotEraseLaterWorkerOutcomesFromTheStructuredSnapshot() {
            var writer = new FailingWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None);
            var run = new LiveRun(new[] { new RunItem("last worker"), new RunItem("queued") }, new LiveRunOptions {
                ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new InlineOutput(writer, profile), CaptureConsole = false, HandleCancelKey = false,
                    WindowSource = () => new[] { 0, 0, 80, 24 } }
            });
            try {
                Assert.True(System.Threading.SpinWait.SpinUntil(() => run.Session.FramesRendered > 0, 5000));
                writer.Fail = true;
                Assert.True(System.Threading.SpinWait.SpinUntil(() => run.OutputFailure != null, 5000));
                run.Started(0, "draining"); run.Finished(0, StatusKind.Fail, "actual worker result", TimeSpan.FromSeconds(7));
                Assert.Throws<InvalidOperationException>(() => run.Complete());
                RunSnapshot snapshot = run.Snapshot();
                Assert.True(snapshot.IsFinished); Assert.Equal(1, snapshot.Failed); Assert.Equal(1, snapshot.Skipped);
                Assert.Equal("actual worker result", snapshot.Items[0].Message); Assert.Equal(TimeSpan.FromSeconds(7), snapshot.Items[0].Duration);
            } finally { run.Dispose(); }
        }

        private sealed class FailingWriter : ITerminalWriter {
            internal volatile bool Fail;
            public bool IsConsole => false;
            public void Write(string text) { if (Fail) { throw new System.IO.IOException("Closed output sink"); } }
            public void Flush() { }
            public void Dispose() { }
        }

        [Fact]
        public void ClosingLogWriteFailureRemainsFatalWhenTheSummarySucceeds() {
            var failure = new System.IO.IOException("Closing log failed");
            var text = new System.Text.StringBuilder();
            var writer = new CallbackWriter();
            var target = new FakeTarget();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            using (var session = new LiveSession(target, new LiveSessionOptions {
                Profile = profile, Output = new LineOutput(writer, profile), Summary = TextBlock.Plain("summary survived"),
                CaptureConsole = false, HandleCancelKey = false
            })) {
                target.Applying = _ => session.Stop();
                writer.Callback = value => {
                    if (value.Contains("closing log")) { Assert.False(session.IsRunning); throw failure; }
                    text.Append(value);
                };
                session.Start(); session.Post(new LiveEvent(target.Name, "write", text: "closing log")); session.Stop();
                Assert.Same(failure, session.Failure); Assert.Equal(1, session.EventsApplied);
                Assert.Contains("summary survived", text.ToString());
            }
        }

        [Fact]
        public void ExplicitRecordingFailureDuringCompletionCannotHideBehindSuccessfulOutput() {
            var failure = new System.IO.IOException("Final recording event failed");
            var recording = new SingleWriteFailureRecorder();
            var output = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            var run = new LiveRun(new[] { new RunItem("done"), new RunItem("queued") }, new LiveRunOptions {
                ProgressEvery = 0, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new LineOutput(output, profile), RecordWriter = recording, CaptureConsole = false, HandleCancelKey = false }
            });
            try {
                run.Finished(0, StatusKind.Pass); Assert.True(run.Flush(TimeSpan.FromSeconds(5)));
                recording.FailNext = failure;
                var error = Assert.Throws<InvalidOperationException>(() => run.Complete());
                Assert.Same(failure, error.InnerException); Assert.Same(failure, run.OutputFailure);
                Assert.Equal(1, recording.FailedWrites); Assert.Contains("2 / 2 finished", output.ToString());
                Assert.Equal(1, run.Snapshot().Passed); Assert.Equal(1, run.Snapshot().Skipped);
            } finally { run.Dispose(); }
        }

        [Fact]
        public void ExplicitDeferredSnapshotFailureIsRetainedAfterSuccessfulSummaryAndFlush() {
            var failure = new System.IO.IOException("Final recording snapshot failed");
            var recording = new SingleWriteFailureRecorder();
            var output = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            var slot = new Slot("slot");
            var target = new FakeTarget();
            using (var session = new LiveSession(slot, new LiveSessionOptions {
                Profile = profile, Output = new LineOutput(output, profile), RecordWriter = recording, Clock = () => TimeSpan.Zero,
                Summary = TextBlock.Plain("summary survived"), CaptureConsole = false, HandleCancelKey = false
            })) {
                session.Start(); session.Post(new LiveEvent(slot.Name, "set") { Payload = target });
                Assert.True(session.Flush(TimeSpan.FromSeconds(5)));
                session.Post(new LiveEvent(target.Name, "changed", text: "changed"));
                Assert.True(session.Flush(TimeSpan.FromSeconds(5))); Assert.Null(session.Failure);
                recording.FailNext = failure; session.Stop();
                Assert.Equal(1, recording.FailedWrites); Assert.Same(failure, session.Failure);
                Assert.Contains("summary survived", output.ToString());
            }
        }

        private sealed class SingleWriteFailureRecorder : System.IO.StringWriter {
            internal System.IO.IOException? FailNext;
            internal int FailedWrites;
            public override void WriteLine(string? value) {
                if (FailNext != null) { var failure = FailNext; FailNext = null; FailedWrites++; throw failure; }
                base.WriteLine(value);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RecoverableDescriptionAndInvalidRecordingDiagnosticsDoNotBecomeOutputFailures(bool invalidRecording) {
            var target = new FakeTarget();
            if (!invalidRecording) { target.Description = _ => throw new FormatException("Invalid description"); }
            var output = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            using (var session = new LiveSession(target, new LiveSessionOptions {
                Profile = profile, Output = new LineOutput(output, profile), RecordWriter = new System.IO.StringWriter(),
                Summary = TextBlock.Plain("summary survived"), CaptureConsole = false, HandleCancelKey = false
            })) {
                session.Start(); session.Post(new LiveEvent(target.Name, "tick", value: invalidRecording ? double.NaN : 0, text: "ordinary line")); session.Stop();
                Assert.Null(session.Failure); Assert.Equal(1, session.EventsApplied);
                if (invalidRecording) { Assert.IsType<ArgumentException>(session.LastError); }
                else { Assert.IsType<FormatException>(session.LastError); }
                Assert.Contains("summary survived", output.ToString());
            }
        }

        [Fact]
        public void CancellationLeavesOutputOpenUntilWorkersDrainAndCompletePrintsOneSummary() {
            var writer = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None, mode: OutputMode.Plain);
            using (var run = new LiveRun(new[] { new RunItem("first"), new RunItem("last"), new RunItem("queued") }, new LiveRunOptions {
                Title = "Cancellation test", NoLive = true, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Output = new LineOutput(writer, profile), CaptureConsole = false, HandleCancelKey = false }
            })) {
                run.Started(0); run.Started(1); run.Finished(0, StatusKind.Pass);
                run.Cancel(); Assert.True(run.Cancellation.IsCancellationRequested);
                run.Finished(1, StatusKind.Warn, "last worker drained");
                Assert.True(run.Flush(TimeSpan.FromSeconds(5)));
                Assert.Contains("last worker drained", writer.ToString());
                Assert.DoesNotContain("3 / 3 finished", writer.ToString());
                var final = run.Complete(); run.Complete();
                Assert.Equal(1, final.Passed); Assert.Equal(1, final.Warned); Assert.Equal(1, final.Skipped);
                Assert.Single(Regex.Matches(writer.ToString(), "3 / 3 finished").Cast<Match>());
                Assert.Equal(0, run.Session.EventsDropped);
            }
        }

        [Theory] [InlineData(false)] [InlineData(true)]
        public void ScrollbackUsesTheLogsLiteralFallbackForInvalidMarkup(bool strict) {
            bool before = Markup.Strict;
            try {
                Markup.Strict = strict;
                var writer = new StringTerminalWriter();
                var profile = TerminalProfile.Fixed(60, 24, ColorDepth.TrueColor);
                var log = new LogRegion("log") { Scrollback = true, ShowTime = false };
                using (var session = new LiveSession(log, new LiveSessionOptions {
                    Mode = LiveMode.Inline, Profile = profile, Output = new InlineOutput(writer, profile), CaptureConsole = false,
                    HandleCancelKey = false, PrintFinalState = false, WindowSource = () => new[] { 0, 0, 60, 24 }
                })) {
                    session.Start();
                    session.Post(new LiveEvent("log", "write", text: "[unknown-style]literal identity[/]", isMarkup: true));
                    session.Post(new LiveEvent("log", "write", text: "[red]styled text[/]", isMarkup: true));
                    session.Post(new LiveEvent("log", "ignored", text: "not a log write"));
                    Assert.True(session.Flush(TimeSpan.FromSeconds(5)));
                    Assert.Null(session.Failure); Assert.Equal(0, session.RenderErrors);
                    Assert.Contains("[unknown-style]literal identity[/]", writer.ToString());
                    Assert.Contains("styled text", Regex.Replace(writer.ToString(), "\u001b\\[[0-9;?]*[ -/]*[@-~]", ""));
                    Assert.DoesNotContain("[red]", writer.ToString());
                    Assert.DoesNotContain("not a log write", writer.ToString());
                }
            } finally { Markup.Strict = before; }
        }

        [Theory] [InlineData(20)] [InlineData(60)] [InlineData(120)]
        public void AProblemsOnlyRowKeepsItsEntireLongItemAndTargetInTheSummary(int width) {
            string name = "very-long-identity-0123456789-abcdefghijklmnopqrstuvwxyz-unchanged";
            string target = "target-with-an-equally-long-hostname.example.test";
            var tracker = new RunTracker(new[] { new RunItem(name, target) }, () => TimeSpan.Zero);
            tracker.FinishItem(0, StatusKind.Fail, "full explanation retained"); tracker.Complete();
            var profile = TerminalProfile.Fixed(width, 24, ColorDepth.None);
            string text = FrameComposer.RenderBlock(new RunSummary(tracker), width, Terminal.CreateContext(profile)).ToPlainText();
            // Card wrapping may introduce whitespace and borders, but every identifier character must remain.
            string compact = Regex.Replace(text, "[\\s│|]", "");
            Assert.Contains(name, compact); Assert.Contains(target, compact);
            Assert.Contains("fullexplanationretained", compact);
        }

        [Fact]
        public void AnInteractiveProfileWithoutVirtualTerminalUsesProgressLinesAndNoKeys() {
            var profile = new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Interactive, 60, 24, supportsVirtualTerminal: false);
            var writer = new System.IO.StringWriter();
            using (var run = new LiveRun(new[] { new RunItem("one") }, new LiveRunOptions {
                InteractiveKeys = true, ProgressEvery = 1, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, Target = new WriterTarget(writer, profile), CaptureConsole = false, HandleCancelKey = false }
            })) {
                Assert.False(run.IsLive); Assert.False(run.HandleKey('p'));
                run.Finished(0, StatusKind.Pass); run.Complete();
                Assert.Contains("progress 1/1 · OK1", writer.ToString()); Assert.True(!writer.ToString().Contains("\u001b"), writer.ToString().Replace("\u001b", "<ESC>"));
            }
        }

        [Fact]
        public void AWriteBatchScrollsEveryLineAndRestoresTheDashboardCursor() {
            var writer = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(40, 6, ColorDepth.None);
            var screen = new FakeVtScreen(40, 6, DisplayWidth.Measure);
            var output = new InlineOutput(writer, profile, false);
            output.BeginLive();
            output.RenderFrame(FrameComposer.RenderBlock(TextBlock.Plain("sticky dashboard"), 39, Terminal.CreateContext(profile)));
            output.BeginWriteBatch();
            for (int i = 0; i < 1000; i++) { output.WriteLine(StyledLine.From("done " + i)); }
            output.EndWriteBatch(); screen.Feed(writer.ToString()); writer.Clear();
            Assert.Equal("sticky dashboard", screen.Row(5)); Assert.Equal(5, screen.CursorY);
            string[] completed = screen.Scrollback.Concat(Enumerable.Range(0, 6).Select(screen.Row)).Where(row => row.StartsWith("done ", StringComparison.Ordinal)).ToArray();
            Assert.Equal(Enumerable.Range(0, 1000).Select(i => "done " + i), completed);
            output.EndLive(); screen.Feed(writer.ToString());
            Assert.DoesNotContain("sticky dashboard", Enumerable.Range(0, 6).Select(screen.Row));
        }

        [Theory] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void TinyDashboardRetainsCountsAndTheLatestProblemWhenThereIsRoom(int height) {
            var tracker = new RunTracker(new[] { new RunItem("failed identity") });
            tracker.FinishItem(0, StatusKind.Fail, "failure detail");
            var profile = TerminalProfile.Fixed(60, height, ColorDepth.None);
            var buffer = new CellBuffer(60, height);
            new RunDashboard(tracker).Render(new Region(buffer), Terminal.CreateContext(profile));
            Assert.Contains("1 fail", buffer.ToPlainText());
            if (height > 1) { Assert.Contains("failed identity", buffer.ToPlainText()); Assert.Contains("failure detail", buffer.ToPlainText()); }
        }

        [Fact]
        public void LegacyTrackerUpdatesKeepDeclaredMetadataAndCompletionOrderConsistent() {
            TimeSpan clock = TimeSpan.Zero;
            var tracker = new RunTracker(new[] { new RunItem("declared name"), new RunItem("queued") }, () => clock);
            tracker.Started(0, "legacy label"); clock = TimeSpan.FromSeconds(2);
            tracker.Finished(0, StatusKind.Fail, "legacy label", "timeout", true);
            tracker.Finished(0, StatusKind.Pass, "duplicate"); tracker.Started(0, "restart");
            tracker.Complete();
            RunSnapshot snapshot = tracker.Snapshot();
            Assert.Equal(1, snapshot.Failed); Assert.Equal(1, snapshot.Skipped);
            Assert.True(snapshot.Failures[0].TimedOut); Assert.Equal("declared name", snapshot.Failures[0].Label);
            Assert.Equal("timeout", snapshot.Items[0].Message); Assert.Equal(TimeSpan.FromSeconds(2), snapshot.Items[0].Duration);
            Assert.Equal(new[] { 1, 2 }, snapshot.Items.Select(item => item.CompletionOrder));
        }
    }
}
