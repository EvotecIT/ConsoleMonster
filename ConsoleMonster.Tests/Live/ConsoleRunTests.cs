using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Running many items with a view: input order, failures and timeouts, cancellation and printed lines.</summary>
    [Collection("Live sessions")]
    public sealed class ConsoleRunTests {
        [Fact]
        public async Task ForEachAsync_ReturnsResultsInInputOrder_AndPrintsALinePerItem() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Enumerable.Range(1, 20), async (n, token) => {
                await Task.Delay((20 - n) * 3, token);
                return n * 10;
            }, new RunOptions { ThrottleLimit = 5, Live = false, Print = line => { lock (lines) { lines.Add(line); } } });
            Assert.Equal(Enumerable.Range(1, 20).Select(n => n * 10), results.Select(r => r.Value));
            Assert.All(results, r => Assert.True(r.Succeeded));
            Assert.Equal(21, lines.Count);
            Assert.StartsWith("20 items: 20 passed, 0 failed", lines[20]);
        }

        [Fact]
        public async Task ForEachAsync_ALabelThatThrows_FallsBackToTheItemTitle() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(new[] { 7, 8 }, (n, token) => Task.FromResult(n), new RunOptions {
                Live = false,
                Label = item => throw new InvalidOperationException("bad label"),
                Print = line => { lock (lines) { lines.Add(line); } }
            });
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.Succeeded));
            Assert.Contains(lines, line => line.Contains("7 ("));
        }

        [Fact]
        public async Task ForEachAsync_RetriesAFailedItemAndCountsTheRetry() {
            List<string> lines = new List<string>();
            int[] calls = new int[3];
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(new[] { 0, 1, 2 }, (n, token) => {
                int call = Interlocked.Increment(ref calls[n]);
                if (n == 1 && call < 2) { throw new InvalidOperationException("flaky"); }
                if (n == 2) { throw new InvalidOperationException("broken"); }
                return Task.FromResult(n);
            }, new RunOptions { Live = false, RetryCount = 2, RetryDelay = TimeSpan.FromMilliseconds(5), Print = line => { lock (lines) { lines.Add(line); } } });
            Assert.Equal(new[] { 1, 2, 3 }, results.Select(r => r.Attempts));
            Assert.Equal(new[] { true, true, false }, results.Select(r => r.Succeeded));
            Assert.Equal(new[] { 1, 2, 3 }, calls);
            Assert.Contains(lines, line => line.Contains("1 (retry 1 of 2): flaky"));
            Assert.Contains(lines, line => line.Contains("2 (retry 2 of 2): broken"));
            Assert.Contains("2 retried", lines[lines.Count - 1]);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(5, false)]
        [InlineData(0, true)]
        [InlineData(5, true)]
        public async Task ForEachAsync_CancellationPreventsAnotherFailedAttempt(int delayMilliseconds, bool cancelOnRetryMessage) {
            using CancellationTokenSource stop = new CancellationTokenSource();
            int calls = 0;
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync<int, int>(new[] { 1 }, (_, token) => {
                calls++;
                if (!cancelOnRetryMessage) { stop.Cancel(); }
                throw new InvalidOperationException("failed while cancellation was requested");
            }, new RunOptions {
                Live = false, RetryCount = 1, RetryDelay = TimeSpan.FromMilliseconds(delayMilliseconds),
                Print = line => {
                    lines.Add(line);
                    if (cancelOnRetryMessage && line.Contains("retry 1 of 1")) { stop.Cancel(); }
                }
            }, stop.Token);
            Assert.Equal(1, calls);
            RunResult<int, int> result = Assert.Single(results);
            Assert.Equal(1, result.Attempts);
            Assert.IsAssignableFrom<OperationCanceledException>(result.Error);
            Assert.False(result.Succeeded);
            Assert.Contains("1 skipped", lines[lines.Count - 1]);
        }

        [Fact]
        public void RunView_ShowsRetriedItemsInTheCellsSummaryAndRunningList() {
            RunTracker tracker = new RunTracker(2);
            tracker.Started(0, "web01");
            tracker.Retrying(0, "web01", 2);
            tracker.Started(1, "web02");
            tracker.Finished(1, StatusKind.Pass, "web02");
            RunSnapshot snapshot = tracker.Snapshot();
            Assert.Equal(new[] { 2, 1 }, snapshot.Attempts);
            Assert.Equal(1, snapshot.Retried);
            Assert.Equal(StatusKind.Running, snapshot.States[0]);
            string text = TestTerminal.RenderText(new RunView(tracker) { RetryCount = 3 }, 140);
            Assert.Contains("1 retried", text);
            Assert.Contains("web01 · retry 1 of 3", text);
            Assert.Throws<ArgumentOutOfRangeException>(() => tracker.Retrying(0, "web01", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunOptions { RetryCount = -1 });
        }

        [Fact]
        public async Task ForEachAsync_NoItems_PrintsNothing() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Array.Empty<int>(), (n, token) => Task.FromResult(n), new RunOptions { Live = false, Title = "empty", Print = lines.Add });
            Assert.Empty(results);
            Assert.Empty(lines);
        }

        [Fact]
        public async Task ForEachAsync_AFailureOrTimeout_FailsThatItemOnly() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(new[] { 1, 2, 3 }, async (n, token) => {
                if (n == 2) {
                    throw new InvalidOperationException("two broke");
                }
                if (n == 3) {
                    await Task.Delay(5000, token);
                }
                return n;
            }, new RunOptions { Timeout = TimeSpan.FromMilliseconds(200), Live = false, Print = line => { lock (lines) { lines.Add(line); } } });
            Assert.True(results[0].Succeeded);
            Assert.Equal("two broke", results[1].Error!.Message);
            Assert.True(results[2].TimedOut);
            Assert.Contains(lines, line => line.Contains("two broke"));
            Assert.Contains(lines, line => line.Contains("timed out"));
        }

        [Fact]
        public async Task ForEachAsync_Cancelled_StopsStartingItems() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                int started = 0;
                IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Enumerable.Range(0, 50), async (n, token) => {
                    if (Interlocked.Increment(ref started) == 5) {
                        stop.Cancel();
                    }
                    await Task.Delay(10, token);
                    return n;
                }, new RunOptions { ThrottleLimit = 2, Live = false, Print = line => { } }, stop.Token);
                Assert.True(results.Count < 50);
            }
        }

        [Fact]
        public async Task ForEachAsync_LiveSessionCancellation_ReachesTheBodyAndStopsAdmission() {
            TerminalProfile profile = TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Ascii);
            IReadOnlyList<RunResult<int, bool>> results = await ConsoleRun.ForEachAsync(new[] { 1, 2 }, (item, token) => {
                LiveSession session = Assert.IsType<LiveSession>(LiveSession.Current);
                session.Stop();
                Assert.True(token.IsCancellationRequested);
                token.ThrowIfCancellationRequested();
                return Task.FromResult(true);
            }, new RunOptions {
                Live = true,
                ThrottleLimit = 1,
                Session = new LiveSessionOptions {
                    Profile = profile, Mode = LiveMode.Inline, HandleCancelKey = false, CaptureConsole = false,
                    PrintFinalState = false, Output = new InlineOutput(new StringTerminalWriter(), profile, false)
                }
            });
            RunResult<int, bool> cancelled = Assert.Single(results);
            Assert.IsAssignableFrom<OperationCanceledException>(cancelled.Error);
            Assert.False(cancelled.TimedOut);
        }

        [Fact]
        public void RunView_ShowsSummaryCellsRunningItemsAndFailures() {
            RunTracker tracker = new RunTracker(6, () => TimeSpan.FromSeconds(10));
            tracker.Finished(0, StatusKind.Pass, "a");
            tracker.Finished(1, StatusKind.Fail, "b", "disk full");
            tracker.Started(2, "c");
            RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            RunView view = new RunView(tracker);
            Size size = view.Measure(new Constraints(80, 20), context);
            CellBuffer buffer = new CellBuffer(80, size.Height);
            view.Render(new Region(buffer), context);
            string text = buffer.ToPlainText();
            Assert.Contains("2/6", text);
            Assert.Contains("1 failed", text);
            Assert.Contains("#3", text);
            Assert.Contains("c", text);
            Assert.Contains("b: disk full", text);
        }

        [Fact]
        public void RunView_Finished_ShowsTheDurationAndUpToTenFailures() {
            RunTracker tracker = new RunTracker(6, () => TimeSpan.FromSeconds(12));
            for (int i = 0; i < 6; i++) {
                tracker.Finished(i, StatusKind.Fail, "item" + i, "broken");
            }
            tracker.Complete();
            RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            RunView view = new RunView(tracker);
            Size size = view.Measure(new Constraints(80, 30), context);
            CellBuffer buffer = new CellBuffer(80, size.Height);
            view.Render(new Region(buffer), context);
            string text = buffer.ToPlainText();
            Assert.Contains("in 12s", text);
            Assert.Contains("item5: broken", text);
            Assert.DoesNotContain("more", text);
        }

        [Fact]
        public void SummaryBar_Finished_ShowsTheDurationOnlyWhenEveryCounterFits() {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(120, 5, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            SummaryBar bar = new SummaryBar { Elapsed = TimeSpan.FromSeconds(12) };
            bar.SetProgress(3, 3);
            bar.SetCounter("{0} failed", 1, StatusKind.Fail);
            bar.SetCounter("{0} passed", 1, StatusKind.Pass);
            bar.SetCounter("{0} skipped", 1, StatusKind.Skip);
            Assert.Contains("in 12s", bar.Line(120, context).PlainText);
            string narrow = bar.Line(36, context).PlainText;
            Assert.DoesNotContain("skipped", narrow);
            Assert.DoesNotContain("in 12s", narrow);
            bar.ShowElapsed = false;
            Assert.DoesNotContain("in 12s", bar.Line(120, context).PlainText);
        }

        [Fact]
        public void Tracker_Complete_CountsItemsNeverStartedAsSkipped() {
            RunTracker tracker = new RunTracker(3);
            tracker.Finished(0, StatusKind.Pass, "a");
            tracker.Started(1, "b");
            tracker.Complete();
            RunSnapshot snapshot = tracker.Snapshot();
            Assert.Equal(1, snapshot.Passed);
            Assert.Equal(2, snapshot.Skipped);
            Assert.True(snapshot.IsFinished);
        }
    }
}
