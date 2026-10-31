using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    [Collection("Live sessions")]
    public sealed class LogViewportTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(60, 12, ColorDepth.None, GlyphTier.Ascii));

        [Fact]
        public void FilterShowsOneTagMinimumLevelOrTextAndClearingItRestoresHiddenHistory() {
            var log = new LogViewport("Log") { Height = 8, ShowElapsed = false, Border = BoxStyle.None };
            log.Write("index build 40%", tag: "sql-index");
            log.Write("cache warm", "verbose", tag: "web");
            log.Write("slow query 2.1 s", "warn", tag: "sql-index");
            log.Write("upload failed", "err", tag: "backup");

            log.Filter = new LogFilter(tag: "SQL-INDEX");
            Assert.Equal(new[] { "index build 40%", "slow query 2.1 s" }, Messages(log));
            // One pinned tag needs no column; the state line names it.
            Assert.StartsWith("index build 40%", Rows(log)[0].Trim());

            log.Filter = new LogFilter(level: "warn");
            Assert.Equal(new[] { "slow query 2.1 s", "upload failed" }, Messages(log));
            Assert.Contains("sql-index", Rows(log)[0]);

            log.Filter = new LogFilter(text: "CACHE");
            Assert.Equal(new[] { "cache warm" }, Messages(log));
            Assert.Contains("Filtered: \"CACHE\"", Rows(log).Last(row => row.Length > 0));

            log.Filter = null;
            Assert.Equal(4, Messages(log).Length);
        }

        [Fact]
        public void PausedViewCountsOnlyShownArrivalsAndEndJumpsBackToTheNewestLine() {
            var log = new LogViewport("Log") { Height = 4, ShowElapsed = false, Border = BoxStyle.None };
            for (int i = 1; i <= 5; i++) { log.Write("step " + i, tag: "job"); }
            string[] following = Rows(log);
            log.Pause();
            // The paused state line takes the last row; the rows above it stay where they were.
            Assert.Equal(following.Take(3), Rows(log).Take(3));
            log.Filter = new LogFilter(tag: "job");
            Assert.True(log.Following);
            log.Pause();
            log.Write("step 6", tag: "job");
            log.Write("noise", tag: "other");
            log.Write("step 7", tag: "job");

            string[] paused = Rows(log);
            Assert.False(log.Following);
            Assert.Equal(2, log.NewLines);
            Assert.Contains("step 5", paused[2]);
            Assert.Contains("Paused", paused[3]);
            Assert.Contains("2 new", paused[3]);

            log.HandleInput(InputEvent.Press(ConsoleKey.End), Context);
            Assert.True(log.Following);
            Assert.Equal(0, log.NewLines);
            Assert.Contains("step 7", Rows(log).Last(row => row.Contains("step")));
        }

        [Fact]
        public void BorderNoneLeavesFramingToTheSurroundingPanel() {
            var log = new LogViewport("Log") { Height = 2, ShowElapsed = false, Border = BoxStyle.None };
            log.Write("ready");
            string frame = FrameComposer.Compose(new Panel(log, "Output"), 30, 4, Context).ToPlainText();
            // One ASCII frame has four corners; the viewport's own frame inside it would add four more.
            Assert.Equal(4, frame.Count(c => c == '+'));
            Assert.Contains("Output", frame);
            Assert.Contains("ready", frame);
        }

        [Fact]
        public void RunningSessionDeliversTaggedWriteEventsAddressedToTheViewportKey() {
            var log = new LogViewport("Output") { ShowElapsed = false };
            using (var session = new LiveSession(new Panel(log, "Output"), ConsoleMonster.Tests.Live.LiveSessionTests.Options(new ConsoleMonster.Tests.Live.FakeOutput(false)))) {
                session.Start();
                session.Post(new LiveEvent("Output", "write", key: "build", text: "CS0618 obsolete", status: "Warning"));
                session.Stop();
                Assert.Equal(0, session.ApplyErrors);
            }
            LogEntry entry = Assert.Single(log.Entries);
            Assert.Equal(("build", "warn", "CS0618 obsolete"), (entry.Tag, entry.Level, entry.Text));
        }

        private static readonly string[] Written = { "index build 40%", "cache warm", "slow query 2.1 s", "upload failed" };

        private static string[] Messages(LogViewport log) {
            string screen = string.Join("\n", Rows(log));
            return Written.Where(message => screen.Contains(message)).ToArray();
        }

        private static string[] Rows(LogViewport log) => FrameComposer.Compose(log, 60, log.Height, Context).ToPlainText().Split('\n').Select(row => row.TrimEnd()).ToArray();
    }
}
