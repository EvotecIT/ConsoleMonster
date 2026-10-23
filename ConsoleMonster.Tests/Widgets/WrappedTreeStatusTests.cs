using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class WrappedTreeStatusTests {
        [Theory]
        [InlineData(GlyphTier.Full, 8, "✗")]
        [InlineData(GlyphTier.Full, 3, "✗")]
        [InlineData(GlyphTier.Ascii, 8, "[FAIL]")]
        public void WrappedTree_ReservesTheStatusBeforeTheGuide(GlyphTier glyphs, int width, string marker) {
            Tree tree = new Tree { WrapLabels = true };
            tree.Add("X", StatusKind.Fail);
            string text = Terminal.RenderText(tree, TerminalProfile.Fixed(width, 20, ColorDepth.None, glyphs), Theme.Dark);
            Assert.Contains(marker + " X", text);
        }
    }
}
