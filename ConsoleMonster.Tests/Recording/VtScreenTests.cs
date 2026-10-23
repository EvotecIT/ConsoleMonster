using System;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class VtScreenTests {
        [Theory]
        [InlineData("H")]
        [InlineData("f")]
        [InlineData("d")]
        public void OriginModePositionsWithinMarginsAndResetReturnsToScreen(string command) {
            VtScreen screen = new VtScreen(12, 6);
            screen.Feed("\x1b[3;5r\x1b[?6h\x1b[1;1" + command + "A\r\x1b[999;1" + command + "B\x1b[?6lC");
            CellBuffer frame = screen.Snapshot();
            Assert.Equal("A", frame[0, 2].Glyph);
            Assert.Equal("B", frame[0, 4].Glyph);
            Assert.Equal("C", frame[0, 0].Glyph);
            screen.Feed("\x1b[?6h\u001bc\x1b[2;4r\x1b[HReset");
            Assert.Equal("Reset", screen.Snapshot().GetRowText(0).TrimEnd());
        }

        [Theory]
        [InlineData("界")]
        [InlineData("\U0001f600")]
        public void InsertAndDeletePreserveWideGlyphAndItsContinuation(string glyph) {
            VtScreen screen = new VtScreen(10, 3);
            screen.Feed("A" + glyph + "BC\r\x1b[@");
            CellBuffer inserted = screen.Snapshot();
            Assert.Equal(glyph, inserted[2, 0].Glyph);
            Assert.Equal(2, inserted[2, 0].Width);
            Assert.True(inserted[3, 0].IsContinuation);
            Assert.Equal("B", inserted[4, 0].Glyph);
            screen.Feed("\r\x1b[P");
            CellBuffer deleted = screen.Snapshot();
            Assert.Equal(glyph, deleted[1, 0].Glyph);
            Assert.Equal(2, deleted[1, 0].Width);
            Assert.True(deleted[2, 0].IsContinuation);
            Assert.Equal("B", deleted[3, 0].Glyph);
            screen.Feed("\r\x1b[2C\x1b[P");
            Assert.False(screen.Snapshot()[1, 0].IsContinuation);
            Assert.NotEqual(glyph, screen.Snapshot()[1, 0].Glyph);
        }

        [Theory]
        [InlineData("\U0001f468\u200d\U0001f469\u200d\U0001f467", 2)]
        [InlineData("\u2764\ufe0f", 2)]
        [InlineData("\U0001f1f5\U0001f1f1", 2)]
        [InlineData("e\u0301", 1)]
        public void GraphemesKeepOneMeasuredCellGroupAcrossByteChunks(string glyph, int width) {
            VtScreen screen = new VtScreen(12, 3);
            foreach (byte value in Encoding.UTF8.GetBytes(glyph + "!")) { screen.Feed(new[] { value }, 1); }
            CellBuffer frame = screen.Snapshot();
            Assert.Equal(glyph, frame[0, 0].Glyph);
            Assert.Equal(width, frame[0, 0].Width);
            Assert.Equal("!", frame[width, 0].Glyph);
        }

        [Fact]
        public void VariationSelectorCanGrowAndWrapTheLastColumn() {
            VtScreen screen = new VtScreen(4, 3);
            screen.Feed("abc\u2764");
            screen.Feed("\ufe0f!");
            CellBuffer frame = screen.Snapshot();
            Assert.Equal("abc ", frame.GetRowText(0));
            Assert.Equal("\u2764\ufe0f", frame[0, 1].Glyph);
            Assert.Equal("!", frame[2, 1].Glyph);
        }

        [Theory]
        [InlineData("\u001b7", "\u001b8")]
        [InlineData("\x1b[s", "\x1b[u")]
        public void AlternateCursorSaveCannotOverwriteMainScreenPosition(string save, string restore) {
            VtScreen screen = new VtScreen(12, 5);
            screen.Feed("Main\x1b[?1049h\x1b[3;4H" + save + "x\x1b[H" + restore + "y\x1b[?1049l!");
            Assert.Equal("Main!", screen.Snapshot().GetRowText(0).TrimEnd());
        }

        [Fact]
        public void AlternateScreenRestoresMainCursorPendingWrap() {
            VtScreen screen = new VtScreen(4, 3);
            screen.Feed("Main\x1b[?1049h\x1b[HAlt\x1b[?1049l!");
            Assert.Equal("Main", screen.Snapshot().GetRowText(0));
            Assert.Equal("!", screen.Snapshot().GetRowText(1).TrimEnd());
        }

        [Fact]
        public void InvalidExtendedPaletteDoesNotAbortExportsOrResetColour() {
            VtScreen screen = new VtScreen(12, 3);
            screen.Feed("\x1b[31mA\x1b[38;5;256mB\x1b[48;5;2147483647mC");
            CellBuffer frame = screen.Snapshot();
            Assert.Equal(frame[0, 0].Style.Foreground, frame[1, 0].Style.Foreground);
            Assert.Equal(frame[0, 0].Style.Background, frame[2, 0].Style.Background);
            SessionRecording cast = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":12,\"height\":3}\n[0,\"o\",\"\\u001b[38;5;256mVisible\"]"));
            Assert.Contains("Visible", cast.ExportFinalSvg());
        }

        [Theory]
        [InlineData("S")]
        [InlineData("T")]
        [InlineData("L")]
        [InlineData("M")]
        public void HugeScrollCountsClearOnlyTheActiveRegion(string operation) {
            VtScreen screen = new VtScreen(8, 5);
            screen.Feed("Top\x1b[5;1HBottom\x1b[2;4r\x1b[2;1HInside\x1b[2;1H\x1b[2147483647" + operation);
            CellBuffer frame = screen.Snapshot();
            Assert.Equal("Top", frame.GetRowText(0).TrimEnd());
            Assert.Equal("Bottom", frame.GetRowText(4).TrimEnd());
            for (int y = 1; y < 4; y++) { Assert.Equal("", frame.GetRowText(y).TrimEnd()); }
        }

        [Fact]
        public void FrameReplayDoesNotRetainHistoryWhileTranscriptStillDoes() {
            string output = string.Concat(System.Linq.Enumerable.Repeat("line\r\n", 2000));
            VtScreen frames = new VtScreen(88, 2, false);
            frames.Feed(output);
            Assert.Equal(2, frames.Transcript().Height);
            VtScreen transcript = new VtScreen(88, 2, true);
            transcript.Feed(output);
            Assert.Equal(2001, transcript.Transcript().Height);
            Assert.Equal("line", transcript.Transcript().GetRowText(0).TrimEnd());
        }
    }
}
