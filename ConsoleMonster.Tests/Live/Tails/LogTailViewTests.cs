using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    public sealed class LogTailViewTests {
        [Fact]
        public void ClosedSessionPostsAreDroppedAndDoNotStrandAReusedView() {
            LogTailView view = new LogTailView();
            using (LiveSession stopped = new LiveSession(view, new LiveSessionOptions {
                CaptureConsole = false, PrintFinalState = false, Discoverable = false
            })) {
                stopped.Stop();
                for (int i = 0; i < 5; i++) { view.Post(stopped, new TailedLogEntry("closed")); }
                Assert.Equal(5, stopped.EventsDropped);
            }
            view.Post(new ImmediateDispatcher(view), new TailedLogEntry("new run"));
            Assert.Equal("new run", Assert.Single(view.Entries).Text);
        }

        [Fact]
        public void CompactViewKeepsMessageHighlightAndQuitHintAfterResizingFromWide() {
            const string original = "2026-10-22T09:31:09Z ERROR database timeout";
            LogTailView view = new LogTailView(highlight: "database");
            Dispatch queue = new Dispatch(view);
            view.Post(queue, LogLineParser.Parse(original));
            queue.Flush();
            Lines(view); // The compact layout must discard the wide timestamp column's previous width.
            CellBuffer buffer = new CellBuffer(60, 4);
            view.Render(new Region(buffer), new RenderContext(TerminalProfile.Fixed(60, 4), Theme.Dark));
            string row = buffer.GetRowText(0).TrimEnd();
            Assert.StartsWith("09:31:09", row);
            Assert.Contains("database timeout", row);
            Assert.DoesNotContain("2026-10-22", row);
            Assert.DoesNotContain("ERROR", row);
            int message = row.IndexOf("database", StringComparison.Ordinal);
            Assert.True((buffer[message, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.Contains("q quit", buffer.GetRowText(3));
            Assert.Contains("End follow", buffer.GetRowText(3));
            Assert.Equal("database timeout", Assert.Single(view.Entries).Text);
        }

        [Fact]
        public void UpPausesAndNewLinesPreserveVisibleAnchor() {
            LogTailView view = new LogTailView(10);
            Dispatch queue = new Dispatch(view);
            Add(view, queue, 1, 6);
            Assert.Equal(new[] { "L5", "L6" }, Lines(view).Take(2));
            view.Navigate(queue, InputEvent.Press(ConsoleKey.UpArrow));
            Assert.True(view.Following); // Input only queued a change.
            queue.Flush();
            Assert.False(view.Following);
            Assert.Equal(new[] { "L4", "L5" }, Lines(view).Take(2));
            Add(view, queue, 7, 2);
            Assert.Equal(new[] { "L4", "L5" }, Lines(view).Take(2));
            Assert.Contains("Paused", Lines(view)[2]);
            view.Navigate(queue, InputEvent.Press(ConsoleKey.End));
            queue.Flush();
            Assert.True(view.Following);
            Assert.Equal(new[] { "L7", "L8" }, Lines(view).Take(2));
        }

        [Fact]
        public void HomeKeepsTheOldestSurvivingAnchorWhenCapacityEvictsHistory() {
            LogTailView view = new LogTailView(6);
            Dispatch queue = new Dispatch(view);
            Add(view, queue, 1, 6);
            Lines(view);
            view.Navigate(queue, InputEvent.Press(ConsoleKey.Home));
            queue.Flush();
            Assert.Equal(new[] { "L1", "L2" }, Lines(view).Take(2));
            Add(view, queue, 7, 1);
            Assert.Equal(new[] { "L2", "L3" }, Lines(view).Take(2));
            Assert.False(view.Following);
            Assert.Equal(1, view.DiscardedHistoryLines);
        }

        [Theory]
        [InlineData(ConsoleKey.PageUp, ConsoleKey.PageDown, "L3", "L4")]
        [InlineData(ConsoleKey.UpArrow, ConsoleKey.DownArrow, "L4", "L5")]
        [InlineData(ConsoleKey.Home, ConsoleKey.End, "L1", "L2")]
        public void NavigationResumesWhenItReachesLatest(ConsoleKey back, ConsoleKey forward, string first, string second) {
            LogTailView view = new LogTailView();
            Dispatch queue = new Dispatch(view);
            Add(view, queue, 1, 6);
            Lines(view);
            view.Navigate(queue, InputEvent.Press(back));
            queue.Flush();
            Assert.Equal(new[] { first, second }, Lines(view).Take(2));
            for (int i = 0; i < 6 && !view.Following; i++) {
                view.Navigate(queue, InputEvent.Press(forward));
                queue.Flush();
            }
            Assert.True(view.Following);
            Assert.Equal(new[] { "L5", "L6" }, Lines(view).Take(2));
        }

        [Fact]
        public void PendingUpdatesAreLosslessAndOnlyAppliedHistoryEvictionsAreCounted() {
            LogTailView view = new LogTailView(3);
            Dispatch queue = new Dispatch(view);
            for (int i = 1; i <= 6; i++) { view.Post(queue, new TailedLogEntry("L" + i)); }
            Assert.Empty(view.Entries);
            queue.Flush();
            Assert.Equal(new[] { "L4", "L5", "L6" }, view.Entries.Select(e => e.Text));
            Assert.Equal(3, view.DiscardedHistoryLines);
            Add(view, queue, 7, 1);
            Assert.Equal(4, view.DiscardedHistoryLines);
            string frame = string.Join("\n", Lines(view));
            Assert.Contains("4 history lines discarded", frame);
            Assert.DoesNotContain("updates dropped", frame);
        }

        [Fact]
        public void ConcurrentPostsApplyEveryUpdateBeforeBoundedHistoryEviction() {
            LogTailView view = new LogTailView(32);
            Dispatch queue = new Dispatch(view);
            Parallel.For(0, 1000, i => view.Post(queue, new TailedLogEntry("L" + i)));
            queue.Flush();
            Assert.Equal(32, view.Entries.Count);
            Assert.Equal(968, view.DiscardedHistoryLines);
            Assert.Equal(32, view.Entries.Select(e => e.Text).Distinct().Count());
        }

        [Fact]
        public void UnsupportedKeysAndResizeDoNotAlterFollowState() {
            LogTailView view = new LogTailView();
            Dispatch queue = new Dispatch(view);
            Assert.False(view.Navigate(queue, InputEvent.Character('x')));
            Assert.False(view.Navigate(queue, InputEvent.Press(ConsoleKey.UpArrow, KeyModifiers.Control)));
            Assert.False(view.Navigate(queue, InputEvent.Resized(20, 4)));
            Assert.Empty(queue.Pending);
            Assert.True(view.Following);
        }

        [Fact]
        public void SourceTimeAndUnknownTimeAlignWithoutInventingClockOrInterpretingMarkup() {
            LogTailView view = new LogTailView();
            Dispatch queue = new Dispatch(view);
            DateTimeOffset source = new DateTimeOffset(2026, 10, 22, 9, 30, 0, TimeSpan.FromHours(2));
            view.Post(queue, new TailedLogEntry("[red]first[/]", "warn", source));
            view.Post(queue, new TailedLogEntry("[green]second[/]", "info"));
            queue.Flush();
            string[] frame = Lines(view);
            Assert.StartsWith("09:30:00", frame[0]);
            Assert.Equal(frame[0].IndexOf("[red]", StringComparison.Ordinal), frame[1].IndexOf("[green]", StringComparison.Ordinal));
            Assert.StartsWith(new string(' ', 11), frame[1]);
            Assert.Equal(source, view.Entries.First().Timestamp);
            Assert.Null(view.Entries.Last().Timestamp);
            Assert.All(view.Entries, entry => Assert.Null(entry.RichText));
        }

        [Fact]
        public void LiteralHighlightEmphasizesAllCaseInsensitiveMatchesWithoutChangingTextOrSourceTime() {
            LogTailView view = new LogTailView(highlight: "[RED]");
            Dispatch queue = new Dispatch(view);
            string text = "[red]one[/] [RED]two[/]";
            view.Post(queue, new TailedLogEntry(text));
            queue.Flush();
            CellBuffer buffer = new CellBuffer(160, 4);
            view.Render(new Region(buffer), new RenderContext(TerminalProfile.Fixed(160, 4), Theme.Light));
            Assert.Equal(text, buffer.GetRowText(0).TrimEnd());
            Assert.True((buffer[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.True((buffer[12, 0].Style.Decoration & TextDecoration.Underline) != 0);
            Assert.Equal(TextDecoration.None, buffer[5, 0].Style.Decoration);
            Assert.Equal(text, Assert.Single(view.Entries).Text);
            Assert.Null(Assert.Single(view.Entries).Timestamp);
            Assert.Null(Assert.Single(view.Entries).RichText);
        }

        [Fact]
        public void LabelsFollowProfileAndUnknownLanguageUsesEnglishFallback() {
            LogTailView view = new LogTailView();
            TerminalProfile profile = TerminalProfile.Fixed(160, 4, ColorDepth.None, GlyphTier.Ascii)
                .WithLanguage(CultureInfo.GetCultureInfo("ja-JP"));
            CellBuffer frame = new CellBuffer(160, 4);
            view.Render(new Region(frame), new RenderContext(profile, Theme.Light));
            Assert.Contains(WidgetLabels.For(profile.Language).TailFollowing, frame.GetRowText(2));
            Assert.Contains("q quit", frame.GetRowText(3));
            Assert.Equal("Following latest", WidgetLabels.For(profile.Language).TailFollowing);
            Assert.Equal("Following latest", LabelCatalog.Entries("en")["Widget.TailFollowing"]);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(20, 2)]
        [InlineData(50, 4)]
        public void SmallRegionsClipWithoutCrashingOrLosingTextualState(int width, int height) {
            LogTailView view = new LogTailView();
            CellBuffer buffer = new CellBuffer(width, height);
            view.Render(new Region(buffer), new RenderContext(TerminalProfile.Fixed(width, height), Theme.Dark));
            Assert.StartsWith("F", buffer.GetRowText(height >= 3 ? height - 2 : height - 1));
        }

        private static void Add(LogTailView view, Dispatch queue, int start, int count) {
            for (int i = start; i < start + count; i++) { view.Post(queue, new TailedLogEntry("L" + i)); }
            queue.Flush();
        }

        private static string[] Lines(LogTailView view) {
            CellBuffer buffer = new CellBuffer(160, 4);
            view.Render(new Region(buffer), new RenderContext(TerminalProfile.Fixed(160, 4, ColorDepth.None, GlyphTier.Ascii), Theme.Dark));
            return Enumerable.Range(0, 4).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
        }

        private sealed class Dispatch : ILiveDispatcher {
            private readonly LogTailView _view;
            internal readonly ConcurrentQueue<LiveEvent> Pending = new ConcurrentQueue<LiveEvent>();
            internal Dispatch(LogTailView view) { _view = view; }
            public void Post(LiveEvent liveEvent) => Pending.Enqueue(liveEvent);
            internal void Flush() { while (Pending.TryDequeue(out LiveEvent? next)) { _view.Apply(next); } }
        }
    }
}
