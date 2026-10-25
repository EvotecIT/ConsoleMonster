using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    public sealed class TailPresentationTests {
        [Theory]
        [InlineData("Dark")]
        [InlineData("Light")]
        [InlineData("HighContrast")]
        public void FooterKeysUseAccentAndPausedHistoryUsesWarningWithoutZeroCounts(string name) {
            ITheme theme = Theme.FromName(name);
            LogTailView view = new LogTailView();
            CellBuffer following = Render(view, 60, theme);
            Assert.Equal("Following latest", following.GetRowText(2).TrimEnd());
            Assert.Equal("q quit · PgUp history · End follow", following.GetRowText(3).TrimEnd());
            RenderContext context = new RenderContext(TerminalProfile.Fixed(60, 4), theme);
            Assert.Equal(context.Style(ThemeToken.Accent), following[0, 3].Style);
            Assert.Equal(context.Style(ThemeToken.Muted), following[2, 3].Style);
            Assert.Equal(context.Style(ThemeToken.Accent), following[9, 3].Style);
            view.Apply(new LiveEvent(view.WidgetId, "home"));
            CellBuffer paused = Render(view, 60, theme);
            Assert.Equal("⏸ Paused · viewing history", paused.GetRowText(2).TrimEnd());
            Assert.Equal(context.Style(ThemeToken.Warn), paused[0, 2].Style);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void TimestampWidthDependsOnRetainedSourceDaysAndNeverTerminalWidth(int width) {
            LogTailView view = new LogTailView(2);
            DateTimeOffset time = new DateTimeOffset(2026, 10, 22, 23, 59, 59, TimeSpan.FromHours(2));
            Append(view, new TailedLogEntry("first", timestamp: time));
            Assert.StartsWith("23:59:59", Render(view, width).GetRowText(0));
            Append(view, new TailedLogEntry("second", timestamp: time.AddSeconds(1)));
            CellBuffer acrossDays = Render(view, width);
            Assert.StartsWith("2026-10-22 23:59:59", acrossDays.GetRowText(0));
            Assert.StartsWith("2026-10-23 00:00:00", acrossDays.GetRowText(1));
            Append(view, new TailedLogEntry("third", timestamp: time.AddSeconds(2)));
            Assert.StartsWith("00:00:00", Render(view, width).GetRowText(0));
            Assert.Equal(time.AddSeconds(1), view.Entries.First().Timestamp);
        }

        [Fact]
        public void PausedCounterCountsAppliedMultilineRowsAndClearsOnEveryReturnToFollowing() {
            LogTailView view = new LogTailView(2);
            view.Apply(new LiveEvent(view.WidgetId, "home"));
            Pending queue = new Pending();
            view.Post(queue, new TailedLogEntry("one\r\ntwo\nthree"));
            Assert.DoesNotContain("new lines", Render(view, 120).GetRowText(2));
            view.Apply(queue.Event!);
            string paused = Render(view, 120).GetRowText(2);
            Assert.Contains("3 new lines", paused);
            Assert.Contains("1 history lines discarded", paused);
            view.Apply(new LiveEvent(view.WidgetId, "follow"));
            Assert.DoesNotContain("new lines", Render(view, 120).GetRowText(2));
            view.Apply(new LiveEvent(view.WidgetId, "home"));
            Append(view, new TailedLogEntry("four"));
            Assert.Contains("1 new lines", Render(view, 120).GetRowText(2));
            view.Apply(new LiveEvent(view.WidgetId, "page-down"));
            Assert.True(view.Following);
            view.Apply(new LiveEvent(view.WidgetId, "home"));
            Assert.DoesNotContain("new lines", Render(view, 120).GetRowText(2));
        }

        private static void Append(LogTailView view, TailedLogEntry entry) => view.Apply(new LiveEvent(view.WidgetId, "append") { Payload = entry });

        private static CellBuffer Render(LogTailView view, int width, ITheme? theme = null) {
            CellBuffer frame = new CellBuffer(width, 4);
            view.Render(new Region(frame), new RenderContext(TerminalProfile.Fixed(width, 4), theme ?? Theme.Dark));
            return frame;
        }

        private sealed class Pending : ILiveDispatcher {
            internal LiveEvent? Event;
            public void Post(LiveEvent liveEvent) => Event = liveEvent;
        }
    }
}
