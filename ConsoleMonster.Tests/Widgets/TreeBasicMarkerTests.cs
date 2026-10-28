using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class TreeBasicMarkerTests {
        [Theory]
        [InlineData(GlyphTier.Full, false, false, "⇄")]
        [InlineData(GlyphTier.Basic, false, false, "<->")]
        [InlineData(GlyphTier.Ascii, false, false, "?")]
        [InlineData(GlyphTier.Full, true, false, "⇄")]
        [InlineData(GlyphTier.Basic, true, false, "<->")]
        [InlineData(GlyphTier.Ascii, true, false, "?")]
        [InlineData(GlyphTier.Full, false, true, "⇄")]
        [InlineData(GlyphTier.Basic, false, true, "<->")]
        [InlineData(GlyphTier.Ascii, false, true, "?")]
        public void Render_BasicMarker_OnlyOverridesBasicAndKeepsIdentityAndStatusTone(GlyphTier tier, bool columns, bool wrap, string marker) {
            Tree tree = new Tree { WrapLabels = wrap };
            if (columns) tree.ValueColumns.Add(new TableColumn("State"));
            TreeNode node = tree.Add("OriginalName", StatusKind.Info);
            node.Marker = "⇄";
            node.BasicMarker = "<->";
            node.AsciiMarker = "?";
            Review184Capture capture = new Review184Capture(tree, 60, tier, ColorDepth.Ansi16);
            string raw = capture.Raw.ToPlainText();
            Assert.Contains(marker + " OriginalName", raw);
            Assert.Contains(marker + " OriginalName", capture.Decoded);
            int x = raw.IndexOf(marker, System.StringComparison.Ordinal);
            Assert.Equal(capture.Context.Style(StatusKind.Info).Color, capture.Raw[x, 0].Style.Color);
            Assert.Equal(StatusKind.Info, node.Status);
        }

        [Theory]
        [InlineData(GlyphTier.Full, false, "↺", "↪", "▸")]
        [InlineData(GlyphTier.Basic, false, "@", "@", ">")]
        [InlineData(GlyphTier.Ascii, false, "@", "@", ">")]
        [InlineData(GlyphTier.Full, true, "↺", "↪", "▸")]
        [InlineData(GlyphTier.Basic, true, "@", "@", ">")]
        [InlineData(GlyphTier.Ascii, true, "@", "@", ">")]
        public void Render_ReferenceMarkers_BasicAndAsciiUseSafeReferencesWhileFullIsUnchanged(GlyphTier tier, bool wrap, string cycle, string repeat, string collapsed) {
            Tree tree = new Tree { WrapLabels = wrap };
            if (!wrap) tree.ValueColumns.Add(new TableColumn("State"));
            tree.Add("CycleName").Reference = TreeReferenceKind.Cycle;
            tree.Add("RepeatName").Reference = TreeReferenceKind.Repeat;
            tree.Add("CollapsedName").Reference = TreeReferenceKind.Collapsed;
            Review184Capture capture = new Review184Capture(tree, 60, tier, ColorDepth.None);
            string text = capture.Decoded;
            Assert.Contains(cycle, text);
            Assert.Contains(repeat, text);
            Assert.Contains(collapsed, text);
            Assert.Contains("cycle", text);
            Assert.Contains("shown above", text);
            Assert.Contains("collapsed", text);
            Assert.Contains("CycleName", text);
            Assert.Contains("RepeatName", text);
            Assert.Contains("CollapsedName", text);
            if (tier != GlyphTier.Full) {
                Assert.DoesNotContain("↺", text);
                Assert.DoesNotContain("↪", text);
                Assert.DoesNotContain("▸", text);
            }
        }
    }
}
