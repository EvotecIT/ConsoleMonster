using System;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The inline surface's in-place redraws and a prompt over a running live dashboard.</summary>
    [Collection("Live sessions")]
    public sealed class SurfaceTests {
        private static CellBuffer Frame(params string[] rows) {
            CellBuffer buffer = new CellBuffer(30, rows.Length);
            for (int y = 0; y < rows.Length; y++) {
                buffer.Write(0, y, rows[y], Style.Plain);
            }
            return buffer;
        }

        [Fact]
        public void Present_SecondFrame_WritesOnlyTheChangedRow() {
            StringTerminalWriter writer = new StringTerminalWriter();
            InlineSurface surface = new InlineSurface(TerminalProfile.Fixed(40, 20, ColorDepth.None), writer, sizeSource: () => new[] { 40, 20 });
            surface.Present(Frame("first", "second", "third"), null);
            string initial = writer.ToString();
            Assert.Contains("first", initial);
            Assert.Contains("third", initial);
            int before = writer.ToString().Length;
            surface.Present(Frame("first", "CHANGED", "third"), null);
            string delta = writer.ToString().Substring(before);
            Assert.Contains("CHANGED", delta);
            Assert.DoesNotContain("first", delta);
            Assert.DoesNotContain("third", delta);
            Assert.Contains("\u001b[1A", delta);
        }

        [Fact]
        public void Complete_ReplacesTheRegionWithTheSummary() {
            StringTerminalWriter writer = new StringTerminalWriter();
            InlineSurface surface = new InlineSurface(TerminalProfile.Fixed(40, 20, ColorDepth.None), writer, sizeSource: () => new[] { 40, 20 });
            surface.Present(Frame("a", "b", "c"), new Caret(2, 1));
            int before = writer.ToString().Length;
            surface.Complete(new[] { StyledLine.From("done") });
            string end = writer.ToString().Substring(before);
            Assert.Contains("\u001b[1A", end);
            Assert.Contains("\u001b[J", end);
            Assert.Contains("done\r\n", end);
            Assert.EndsWith("\u001b[?25h", end);
        }

        [Fact]
        public void Present_ShorterFrame_ClearsTheRowsBelow() {
            StringTerminalWriter writer = new StringTerminalWriter();
            InlineSurface surface = new InlineSurface(TerminalProfile.Fixed(40, 20, ColorDepth.None), writer, sizeSource: () => new[] { 40, 20 });
            surface.Present(Frame("a", "b", "c", "d"), null);
            int before = writer.ToString().Length;
            surface.Present(Frame("a", "b"), null);
            Assert.Contains("\u001b[J", writer.ToString().Substring(before));
        }

        [Fact]
        public void Prompt_DuringALiveSession_PausesTheDashboardAndResumes() {
            FakeOutput output = new FakeOutput();
            TextBlock widget = new TextBlock("dashboard");
            using (LiveSession session = new LiveSession(widget, new LiveSessionOptions { Output = output, Profile = TerminalProfile.Fixed(40, 12, ColorDepth.None), FramesPerSecond = 50 })) {
                session.Start();
                SpinWait.SpinUntil(() => output.Frames > 0, 2000);
                InteractiveOptions options = new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ConsoleMonster.Input.ScriptedInput("y") { Paced = true }, Profile = TerminalProfile.Fixed(40, 12, ColorDepth.None), Theme = Theme.Dark };
                int before = output.Invalidations;
                bool answer = new ConfirmPrompt("Continue?").Show(options);
                Assert.True(answer);
                Assert.False(session.IsPaused);
                Assert.True(SpinWait.SpinUntil(() => output.Invalidations > before, 2000));
            }
        }

        [Fact]
        public void Pause_StopsFramesUntilDisposed() {
            FakeOutput output = new FakeOutput();
            using (LiveSession session = new LiveSession(new TextBlock("x"), new LiveSessionOptions { Output = output, Profile = TerminalProfile.Fixed(20, 3, ColorDepth.None), FramesPerSecond = 50 })) {
                session.Start();
                SpinWait.SpinUntil(() => output.Frames > 0, 2000);
                using (LivePause pause = session.Pause()) {
                    Thread.Sleep(60);
                    int frames = output.Frames;
                    Thread.Sleep(150);
                    Assert.Equal(frames, output.Frames);
                    pause.Present(new CellBuffer(20, 3));
                    Assert.Equal(frames + 1, output.Frames);
                }
                int resumed = output.Frames;
                SpinWait.SpinUntil(() => output.Frames > resumed, 2000);
                Assert.True(output.Frames > resumed);
            }
        }
    }
}
