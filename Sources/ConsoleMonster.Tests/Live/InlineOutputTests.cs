using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>
    /// The inline live area: drawn below the cursor in the normal screen, lines inserted above it, shrinking and growing,
    /// staying at the bottom while output scrolls, and leaving the screen clean for the final state.
    /// </summary>
    public sealed class InlineOutputTests {
        private static CellBuffer Frame(params string[] rows) {
            CellBuffer buffer = new CellBuffer(30, rows.Length);
            for (int y = 0; y < rows.Length; y++) {
                buffer.Write(0, y, rows[y], Style.Plain);
            }
            return buffer;
        }

        private static string[] Rows(FakeVtScreen screen) {
            string[] rows = new string[screen.Height];
            for (int y = 0; y < screen.Height; y++) {
                rows[y] = screen.Row(y);
            }
            return rows;
        }

        private static void Pump(StringTerminalWriter writer, FakeVtScreen screen) {
            screen.Feed(writer.ToString());
            writer.Clear();
        }

        [Fact]
        public void LinesWrittenDuringTheSession_GoAboveTheArea() {
            StringTerminalWriter writer = new StringTerminalWriter();
            FakeVtScreen screen = new FakeVtScreen(30, 8, DisplayWidth.Measure);
            screen.Feed("PS> run\r\n");
            InlineOutput output = new InlineOutput(writer, TerminalProfile.Fixed(30, 8, ColorDepth.None), false);
            output.BeginLive();
            output.RenderFrame(Frame("build  20%", "lane a"));
            Pump(writer, screen);
            Assert.Equal(new[] { "PS> run", "build  20%", "lane a", "", "", "", "", "" }, Rows(screen));
            output.WriteLine(StyledLine.From("log one"));
            output.WriteLine(StyledLine.From("log two"));
            output.RenderFrame(Frame("build  60%", "lane a", "lane b"));
            Pump(writer, screen);
            Assert.Equal(new[] { "PS> run", "log one", "log two", "build  60%", "lane a", "lane b", "", "" }, Rows(screen));
            output.EndLive();
            output.WriteBlock(Frame("build 100%"));
            Pump(writer, screen);
            Assert.Equal(new[] { "PS> run", "log one", "log two", "build 100%", "", "", "", "" }, Rows(screen));
        }

        [Fact]
        public void AShrinkingArea_ErasesTheRowsItLeft() {
            StringTerminalWriter writer = new StringTerminalWriter();
            FakeVtScreen screen = new FakeVtScreen(30, 6, DisplayWidth.Measure);
            InlineOutput output = new InlineOutput(writer, TerminalProfile.Fixed(30, 6, ColorDepth.None), false);
            output.BeginLive();
            output.RenderFrame(Frame("one", "two", "three"));
            output.RenderFrame(Frame("only"));
            Pump(writer, screen);
            Assert.Equal(new[] { "only", "", "", "", "", "" }, Rows(screen));
            Assert.Equal(1, output.Height);
            output.RenderFrame(Frame("only", "again"));
            Pump(writer, screen);
            Assert.Equal(new[] { "only", "again", "", "", "", "" }, Rows(screen));
        }

        [Fact]
        public void ManyLines_ScrollAboveWhileTheAreaStaysAtTheBottom() {
            StringTerminalWriter writer = new StringTerminalWriter();
            FakeVtScreen screen = new FakeVtScreen(30, 5, DisplayWidth.Measure);
            InlineOutput output = new InlineOutput(writer, TerminalProfile.Fixed(30, 5, ColorDepth.None), false);
            output.BeginLive();
            output.RenderFrame(Frame("progress", "status"));
            for (int i = 1; i <= 8; i++) {
                output.WriteLine(StyledLine.From("line " + i));
                Pump(writer, screen);
            }
            Assert.Equal(new[] { "line 6", "line 7", "line 8", "progress", "status" }, Rows(screen));
            Assert.Equal(new[] { "line 1", "line 2", "line 3", "line 4", "line 5" }, screen.Scrollback.ToArray());
        }
    }

    /// <summary>An inline live session end to end, in the live-session collection because it captures the console.</summary>
    [Collection("Live sessions")]
    public sealed class InlineSessionTests {
        [Fact]
        public void InlineSession_PrintsCapturedOutputAboveAndLeavesTheFinalState() {
            StringTerminalWriter writer = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(40, 10, ColorDepth.None);
            ProgressBar progress = new ProgressBar("Build", "build") { Total = 4 };
            LiveSessionOptions options = new LiveSessionOptions {
                Mode = LiveMode.Inline,
                Output = new InlineOutput(writer, profile, false),
                Profile = profile,
                HandleCancelKey = false,
                FramesPerSecond = 60,
                WindowSource = () => new[] { 0, 0, 40, 10 }
            };
            using (LiveSession session = new LiveSession(new Rows(progress), options)) {
                session.Start();
                Console.WriteLine("compiling project");
                session.Post(new LiveEvent("build", "advance", value: 2));
                session.Post(new LiveEvent("$log", "write", text: "a log line"));
                SpinWait.SpinUntil(() => writer.ToString().Contains("a log line") && writer.ToString().Contains("50%"), 3000);
                session.Post(new LiveEvent("build", "advance", value: 2));
                session.Stop();
            }
            FakeVtScreen screen = new FakeVtScreen(40, 10, DisplayWidth.Measure);
            screen.Feed(writer.ToString());
            string[] rows = new string[10];
            for (int y = 0; y < 10; y++) {
                rows[y] = screen.Row(y);
            }
            Assert.Equal("compiling project", rows[0]);
            Assert.Equal("a log line", rows[1]);
            Assert.StartsWith("Build", rows[2]);
            Assert.Contains("100%", rows[2]);
            Assert.Equal(string.Empty, rows[3]);
        }
    }
}
