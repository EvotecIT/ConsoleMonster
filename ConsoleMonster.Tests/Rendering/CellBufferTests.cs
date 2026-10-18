using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Wide graphemes must never be split: a split cell would shift every later column on the terminal.</summary>
    public sealed class CellBufferTests {
        [Fact]
        public void WideGraphemeThatDoesNotFitAtTheEdgeBecomesBlank() {
            CellBuffer buffer = new CellBuffer(3, 1);
            buffer.Write(2, 0, "日", Style.Plain);
            Assert.Equal(" ", buffer[2, 0].Glyph);
            Assert.Equal(1, buffer[2, 0].Width);
        }

        [Fact]
        public void OverwritingHalfOfAWideGraphemeClearsTheOtherHalf() {
            CellBuffer buffer = new CellBuffer(4, 1);
            buffer.Write(0, 0, "日", Style.Plain);
            buffer.Set(1, 0, "x", Style.Plain);
            Assert.Equal(" ", buffer[0, 0].Glyph);
            Assert.Equal("x", buffer[1, 0].Glyph);
            Assert.Equal(" x", buffer.GetRowText(0).Substring(0, 2));
        }

        [Fact]
        public void RegionClipsWritesToItsBounds() {
            CellBuffer buffer = new CellBuffer(10, 3);
            Region region = new Region(buffer).Sub(2, 1, 4, 1);
            region.Write(0, 0, "abcdefgh", Style.Plain);
            region.Write(0, 1, "zzz", Style.Plain);
            Assert.Equal("  abcd", buffer.GetRowText(1).TrimEnd());
            Assert.Equal(string.Empty, buffer.GetRowText(2).Trim());
        }
    }
}
