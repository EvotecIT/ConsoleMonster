using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Contracts of live VT rendering: diffing, synchronized frames, no writes to the scrolling corner, clean restore.</summary>
    public sealed class VtOutputTests {
        private static readonly TerminalProfile Profile = TerminalProfile.Fixed(20, 5, ColorDepth.TrueColor, GlyphTier.Full);

        [Fact]
        public void UnchangedFrameWritesNoCells() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, Profile, false)) {
                CellBuffer frame = new CellBuffer(20, 5);
                frame.Write(0, 0, "hello", new Style(Color.FromRgb(1, 2, 3)));
                output.BeginLive();
                output.RenderFrame(frame);
                writer.Clear();
                output.RenderFrame(frame);
                Assert.Equal("\u001b[?2026h\u001b[0m\u001b[?2026l", writer.ToString());
            }
        }

        [Fact]
        public void ChangedCellIsRewrittenAtItsPositionOnly() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                CellBuffer frame = new CellBuffer(20, 5);
                frame.Write(0, 2, "count: 1", Style.Plain);
                output.BeginLive();
                output.RenderFrame(frame);
                writer.Clear();
                frame.Write(7, 2, "2", Style.Plain);
                output.RenderFrame(frame);
                Assert.Equal("\u001b[?2026h\u001b[3;8H2\u001b[?2026l", writer.ToString());
            }
        }

        [Fact]
        public void BottomRightCellIsNeverWritten() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                CellBuffer frame = new CellBuffer(20, 5);
                frame.Write(0, 4, new string('x', 20), Style.Plain);
                output.BeginLive();
                writer.Clear();
                output.RenderFrame(frame);
                Assert.Contains("\u001b[5;1H" + new string('x', 19) + "\u001b[?2026l", writer.ToString());
            }
        }

        [Fact]
        public void EndLiveRestoresCursorScreenAndAttributesOnce() {
            StringTerminalWriter writer = new StringTerminalWriter();
            VtOutput output = new VtOutput(writer, Profile, false);
            output.BeginLive();
            writer.Clear();
            output.EndLive();
            output.EndLive();
            output.Dispose();
            Assert.Equal("\u001b[0m\u001b[?25h\u001b[?1049l", writer.ToString());
        }

        [Fact]
        public void FrameDiffMergesNearbyChangesAndKeepsWideGraphemesWhole() {
            CellBuffer before = new CellBuffer(20, 1);
            CellBuffer after = new CellBuffer(20, 1);
            after.Write(2, 0, "a", Style.Plain);
            after.Write(5, 0, "b", Style.Plain);
            after.Write(15, 0, "日", Style.Plain);
            List<DiffRun> runs = FrameDiff.Compute(before, after, 4);
            Assert.Equal(2, runs.Count);
            Assert.Equal(2, runs[0].Start);
            Assert.Equal(4, runs[0].Length);
            Assert.Equal(15, runs[1].Start);
            Assert.Equal(2, runs[1].Length);
        }
    }
}
