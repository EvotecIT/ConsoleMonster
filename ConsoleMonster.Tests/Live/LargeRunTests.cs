using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LargeRunTests {
        private static LiveRunOptions Options(StringTerminalWriter writer, bool live, int width = 120, int height = 24) {
            TerminalProfile profile = TerminalProfile.Fixed(width, height, ColorDepth.None, GlyphTier.Ascii, live ? OutputMode.Interactive : OutputMode.Plain);
            return new LiveRunOptions {
                Title = "Worker run", NoLive = !live, ProgressEvery = 100, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { CaptureConsole = false, HandleCancelKey = false, Discoverable = false, Profile = profile,
                    Output = live ? (ITerminalOutput)new InlineOutput(writer, profile, false) : new LineOutput(writer, profile), WindowSource = () => new[] { 0, 0, width, height } }
            };
        }

        [Fact]
        public async Task EightWorkersKeepAll1200CompletionsExactlyOnceInScrollback() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LiveRun run = new LiveRun(Enumerable.Range(0, 1200).Select(i => new RunItem("item-" + i.ToString("D4"))), Options(writer, true)))
            using (CountdownEvent ready = new CountdownEvent(8))
            using (ManualResetEventSlim go = new ManualResetEventSlim()) {
                Task[] workers = Enumerable.Range(0, 8).Select(worker => Task.Run(() => {
                    ready.Signal(); Assert.True(go.Wait(TimeSpan.FromSeconds(10)));
                    for (int i = worker; i < 1200; i += 8) { run.Started(i, "checking"); run.Finished(i, StatusKind.Pass); run.Finished(i, StatusKind.Fail, "duplicate"); }
                })).ToArray();
                Assert.True(ready.Wait(TimeSpan.FromSeconds(10))); go.Set();
                await Task.WhenAll(workers);
                RunSnapshot snapshot = run.Complete();
                Assert.Equal(1200, snapshot.Passed); Assert.Equal(0, snapshot.Failed);
                Assert.Equal(Enumerable.Range(1, 1200), snapshot.Items.Select(i => i.CompletionOrder).OrderBy(i => i));
                string output = writer.ToString();
                foreach (int i in Enumerable.Range(0, 1200)) { Assert.Single(Regex.Matches(output, "OK\\s+item-" + i.ToString("D4") + " ").Cast<Match>()); }
                Assert.DoesNotContain("\u001b[2J", output);
                string plain = Regex.Replace(output, "\u001b\\[[0-9;?]*[ -/]*[@-~]", "");
                Assert.True(plain.LastIndexOf("item-1199", StringComparison.Ordinal) < plain.LastIndexOf("Worker run · 1200", StringComparison.Ordinal));
                Assert.Equal(0, run.Session.EventsDropped); Assert.Equal(0, run.Session.RenderErrors);
            }
        }

        [Fact]
        public void InsertBatchRedrawsTheDashboardOnceAndKeepsEveryLine() {
            StringTerminalWriter writer = new StringTerminalWriter();
            var profile = TerminalProfile.Fixed(60, 24, ColorDepth.None, GlyphTier.Ascii);
            InlineOutput output = new InlineOutput(writer, profile, false);
            output.BeginLive();
            var frame = FrameComposer.RenderBlock(new TextBlock("sticky dashboard"), 59, Terminal.CreateContext(profile));
            output.RenderFrame(frame); writer.Clear();
            output.BeginWriteBatch();
            for (int i = 0; i < 1000; i++) { output.WriteLine(ConsoleMonster.Text.StyledLine.From("finished " + i)); }
            output.EndWriteBatch();
            string text = writer.ToString();
            Assert.Single(Regex.Matches(text, "sticky dashboard").Cast<Match>());
            Assert.Equal(1000, Regex.Matches(text, "finished ").Count);
            Assert.StartsWith("\u001b[?2026h", text); Assert.EndsWith("\u001b[?2026l", text);
            output.EndLive();
        }

        [Fact]
        public void InlineHeightLimitTracksTerminalResize() {
            StringTerminalWriter writer = new StringTerminalWriter();
            int height = 50;
            LiveRunOptions options = Options(writer, true, 120, height);
            options.Session!.WindowSource = () => new[] { 0, 0, 120, Volatile.Read(ref height) };
            var inline = (InlineOutput)options.Session.Output!;
            using (LiveRun run = new LiveRun(Enumerable.Range(0, 12).Select(i => new RunItem("check " + i)), options)) {
                Assert.True(SpinWait.SpinUntil(() => inline.Height == 20, 5000));
                Volatile.Write(ref height, 24);
                Assert.True(SpinWait.SpinUntil(() => inline.Height == 9, 5000));
                Assert.Equal(0, run.Session.RenderErrors);
            }
        }

        [Fact]
        public void WarningDetailsAndGroupsSurvivePauseAndCompletion() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LiveRun run = new LiveRun(new[] { new RunItem("one [literal]", "host-1", "Network"), new RunItem("two", "host-2", "Network") }, Options(writer, true))) {
                Assert.True(run.HandleKey('p')); Assert.True(run.HandleKey('g')); Assert.True(run.HandleKey('?'));
                run.Started(0, "collecting"); run.Finished(0, StatusKind.Warn, "Full detail [literal] survives without interpreting markup", TimeSpan.FromSeconds(4));
                RunSnapshot before = run.Snapshot();
                Assert.Equal(1, before.Warned); Assert.Equal("collecting", before.Items[0].Phase);
                Assert.True(run.Dashboard.LogPaused); Assert.True(run.Dashboard.ShowHelp); Assert.False(run.Dashboard.ShowGroups);
                RunSnapshot final = run.Complete();
                Assert.Equal(1, final.Warned); Assert.Equal(1, final.Skipped);
                Assert.Matches("WARN\\s+one \\[literal\\]", writer.ToString());
                Assert.Contains("Full detail [literal]", writer.ToString());
                Assert.Equal(final.Elapsed, run.Complete().Elapsed);
            }
        }

        [Fact]
        public void NonInteractiveCountCadenceIsAppendOnlyAndPreservesTheFinalState() {
            StringTerminalWriter writer = new StringTerminalWriter();
            LiveRunOptions options = Options(writer, false); options.ProgressEvery = 2;
            using (LiveRun run = new LiveRun(Enumerable.Range(0, 3).Select(i => new RunItem("job " + i)), options)) {
                run.Finished(0, StatusKind.Pass); run.Finished(1, StatusKind.Fail, "bad endpoint");
                Assert.False(run.HandleKey('p'));
                RunSnapshot final = run.Complete();
                Assert.Equal(1, final.Skipped);
                string output = writer.ToString();
                Assert.Contains("2/3 · 1 passed · 1 failed", output);
                Assert.Contains("Problems", output); Assert.Contains("bad endpoint", output);
                Assert.Empty(Regex.Matches(output, ".{0,30}\u001b.{0,50}").Cast<Match>().Select(m => m.Value.Replace("\u001b", "<ESC>")));
            }
        }

        [Fact]
        public void NonInteractiveTimerReportsWhileWorkersAreStillRunning() {
            StringTerminalWriter writer = new StringTerminalWriter();
            long ticks = 0;
            LiveRunOptions options = Options(writer, false); options.ProgressEvery = 0; options.ProgressInterval = TimeSpan.FromSeconds(2); options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref ticks));
            using (LiveRun run = new LiveRun(new[] { new RunItem("slow check") }, options)) {
                run.Started(0); Interlocked.Exchange(ref ticks, TimeSpan.FromSeconds(3).Ticks);
                Assert.True(SpinWait.SpinUntil(() => writer.ToString().Contains("0/1 · 0 passed"), 5000));
                Assert.Equal(0, run.Snapshot().Done);
            }
        }

        [Fact]
        public void TrackerKeepsElapsedPhaseAndMovingRateWithoutRestartingFinishedItems() {
            TimeSpan now = TimeSpan.Zero;
            RunTracker tracker = new RunTracker(new[] { new RunItem("A"), new RunItem("B"), new RunItem("C") }, () => now);
            tracker.StartItem(0, "collecting"); now = TimeSpan.FromSeconds(1); tracker.StartItem(0, "evaluating");
            now = TimeSpan.FromSeconds(2); tracker.FinishItem(0, StatusKind.Pass);
            Assert.Equal(TimeSpan.FromSeconds(2), tracker.Snapshot().Items[0].Duration);
            Assert.Equal(0.5, tracker.Snapshot().Rate, 3);
            Assert.False(tracker.StartItem(0)); Assert.Null(tracker.FinishItem(0, StatusKind.Fail));
            now = TimeSpan.FromSeconds(20);
            Assert.True(tracker.Snapshot().Rate < 0.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => tracker.FinishItem(1, StatusKind.Running));
        }
    }
}
