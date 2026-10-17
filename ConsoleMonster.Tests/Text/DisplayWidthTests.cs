using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class DisplayWidthTests {
        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("hello", 5)]
        [InlineData("中文", 4)]
        [InlineData("ＡＢ", 4)]
        [InlineData("한글", 4)]
        [InlineData("e\u0301", 1)]
        [InlineData("\u0301", 0)]
        [InlineData("x\u0488\u20DD", 1)]
        [InlineData("a\u200B\u200C\u2060b", 2)]
        [InlineData("\u00AD", 1)]
        [InlineData("\u1161\u11A8", 0)]
        [InlineData("\u1100\u1161\u11A8", 2)]
        [InlineData("\0\t\r\n\u001B\u007F\u0085", 0)]
        [InlineData("😀", 2)]
        [InlineData("👩‍💻", 2)]
        [InlineData("👨‍👩‍👧‍👦", 2)]
        [InlineData("👍🏽", 2)]
        [InlineData("🇵🇱", 2)]
        [InlineData("🇵🇱🇬🇧", 4)]
        [InlineData("☀", 1)]
        [InlineData("☀\uFE0F", 2)]
        [InlineData("❤\uFE0F", 2)]
        [InlineData("©\uFE0F", 2)]
        [InlineData("1\uFE0F\u20E3", 2)]
        [InlineData("a\uFE0F", 1)]
        [InlineData("a\u200Db", 2)]
        [InlineData("ب\u200Dب", 2)]
        [InlineData("a\U000E0100", 1)]
        public void MeasuresTerminalCells(string? text, int expected) {
            Assert.Equal(expected, DisplayWidth.Measure(text));
        }

        [Fact]
        public void InvalidUtf16UsesReplacementCellWidths() {
            // Attribute metadata cannot round-trip isolated UTF-16 surrogates.
            string text = new string(new[] { '\uD800', 'x', '\uDC00' });
            Assert.Equal(3, DisplayWidth.Measure(text));
            Assert.Equal(3, DisplayWidth.Cells(text).Count());
        }

        [Theory]
        [InlineData(0x0041, 1)]
        [InlineData(0x00AD, 1)]
        [InlineData(0x0301, 0)]
        [InlineData(0x200D, 0)]
        [InlineData(0xFE0F, 0)]
        [InlineData(0xE0100, 0)]
        [InlineData(0x1160, 0)]
        [InlineData(0x11FF, 0)]
        [InlineData(0x1F600, 2)]
        [InlineData(0x4E2D, 2)]
        [InlineData(0x30000, 2)]
        [InlineData(0xFF01, 2)]
        [InlineData(0x2600, 1)]
        [InlineData(0x303F, 1)]
        [InlineData(0x00A1, 1)]
        [InlineData(0x1F700, 1)]
        [InlineData(0x1E08F, 0)]
        [InlineData(0x1FAE9, 2)]
        [InlineData(-1, 1)]
        [InlineData(0x110000, 1)]
        public void MeasuresCodePointsIndependentlyOfRuntimeUnicodeVersion(int codePoint, int expected) {
            Assert.Equal(expected, DisplayWidth.GetWidth(codePoint));
        }

        [Theory]
        [InlineData("e\u0301", 1)]
        [InlineData("😀", 2)]
        [InlineData("👩‍💻", 2)]
        [InlineData("👨‍👩‍👧‍👦", 2)]
        [InlineData("👍🏽", 2)]
        [InlineData("🇵🇱", 2)]
        [InlineData("☀\uFE0F", 2)]
        [InlineData("1\uFE0F\u20E3", 2)]
        [InlineData("\u1100\u1161\u11A8", 2)]
        [InlineData("\u0915\u094D\u0937", 2)]
        public void CellsKeepGraphemesIntact(string text, int width) {
            TextCell cell = Assert.Single(DisplayWidth.Cells(text));
            Assert.Equal(text, cell.Glyph);
            Assert.Equal(width, cell.Width);
        }

        [Fact]
        public void CellsOmitControlsWithoutLosingAdjacentText() {
            List<TextCell> cells = DisplayWidth.Cells("\u0001Ae\u0301\u001BB").ToList();
            Assert.Equal(new[] { "A", "e\u0301", "B" }, cells.Select(cell => cell.Glyph));
            Assert.Equal(3, cells.Sum(cell => cell.Width));
        }

        [Fact]
        public void StyledLineMeasuresAcrossStyleBoundaries() {
            StyledLine line = new StyledLine(new[] {
                new TextSpan("👩", new Style(Color.FromAnsi16(1))),
                new TextSpan("‍💻", new Style(Color.FromAnsi16(2))),
                new TextSpan("e"), new TextSpan("\u0301")
            });
            Assert.Equal(3, line.Width);
            Assert.Equal("👩‍💻e\u0301", line.PlainText);
        }

        [Fact]
        public void MeasuresClustersWithExplicitTerminalProfiles() {
            TerminalWidthProfile split = TerminalWidthProfile.Standard.WithZwjSequences(ZwjRendering.Split);
            Assert.Equal(8, DisplayWidth.Measure("👨‍👩‍👧‍👦", split));
            Assert.Equal(1, DisplayWidth.Measure("😀", TerminalWidthProfile.Standard.WithEmojiWidth(1)));
            Assert.Equal(2, DisplayWidth.Measure("▲", TerminalWidthProfile.Standard.WithAmbiguousWidth(2)));
            Assert.Equal(2, DisplayWidth.Measure("🇵🇱", TerminalWidthProfile.Standard.WithFlagWidth(2)));
            Assert.Equal(2, DisplayWidth.Measure("👍🏽", TerminalWidthProfile.Standard));
            Assert.Equal(2, DisplayWidth.Measure("#️⃣", TerminalWidthProfile.Standard));
            Assert.Equal(1, DisplayWidth.Measure("☺︎", TerminalWidthProfile.Standard));
            Assert.Equal(2, DisplayWidth.Measure("☺️", TerminalWidthProfile.Standard));
        }
    }
}
