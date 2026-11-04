using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class WrappedTreeStatusTests {
        [Theory]
        [InlineData(GlyphTier.Full, "│")]
        [InlineData(GlyphTier.Basic, "│")]
        [InlineData(GlyphTier.Ascii, "|")]
        public void WrappedTree_PreservesAncestorAndSiblingGuidesOnContinuationLines(GlyphTier glyphs, string vertical) {
            var tree = new Tree("Office") { WrapLabels = true };
            TreeNode group = tree.Add("LAN");
            group.Add("File server", detail: "failed two nights RPC unavailable");
            group.Add("Printer");
            tree.Add("Cloud");
            string text = Terminal.RenderText(tree, TerminalProfile.Fixed(26, 40, ColorDepth.None, glyphs), Theme.Dark);
            string[] lines = text.Split('\n');
            int first = System.Array.FindIndex(lines, line => line.Contains("File server"));
            Assert.InRange(first, 0, lines.Length - 2);
            Assert.StartsWith(vertical + "   " + vertical + "   ", lines[first + 1]);
            Assert.DoesNotContain("unavailable", lines[first]);
            Assert.Contains("unavailable", text);
        }
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
