using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class TreeReview184Tests {
        [Theory]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 59)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 60)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 80)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 119)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 59)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 80)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 119)]
        public void Render_ForeignDetailInNotes_PrecedesDomainAndKeepsStateOnItsOwnRow(GlyphTier tier, ColorDepth depth, int width) {
            Tree tree = new Tree { TruncateNotesInMiddle = true };
            tree.ValueColumns.Add(new TableColumn("Domain") { Priority = -1 });
            TreeNode foreign = tree.Add("Foreign.Off", StatusKind.Info, "disabled · cross-forest");
            foreign.Marker = "⇄";
            foreign.BasicMarker = "<->";
            foreign.AsciiMarker = "<->";
            foreign.Values["Domain"] = TableCell.FromValue("partner.example.test");
            TreeNode local = tree.Add("Local.On", StatusKind.Pass, "enabled");
            local.Values["Domain"] = TableCell.FromValue("local.test");
            Review184Capture capture = new Review184Capture(tree, width, tier, depth);
            foreach (string rendered in new[] { capture.Raw.ToPlainText(), capture.Decoded }) {
                string foreignRow = Assert.Single(rendered.Split('\n'), row => row.Contains("Foreign.Off"));
                string localRow = Assert.Single(rendered.Split('\n'), row => row.Contains("Local.On"));
                Assert.Contains("<-> Foreign.Off  disabled · cross-forest", foreignRow);
                Assert.True(foreignRow.IndexOf("disabled", StringComparison.Ordinal) < foreignRow.IndexOf("part", StringComparison.Ordinal), foreignRow);
                Assert.DoesNotContain("enabled", foreignRow);
                Assert.Contains("enabled", localRow);
                Assert.Contains("local.test", localRow);
                Assert.DoesNotContain("disabled", localRow);
                Assert.DoesNotContain("partner", localRow);
            }
            Assert.Equal("partner.example.test", foreign.Values["Domain"].Value);
            Assert.Equal("disabled · cross-forest", foreign.Detail);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Render_UnknownMarkerTone_PreservesUnknownSemanticsAcrossTreeLayouts(bool columns, bool wrapped) {
            Tree tree = new Tree { WrapLabels = wrapped };
            if (columns) tree.ValueColumns.Add(new TableColumn("State"));
            TreeNode node = tree.Add("Unknown.Account", detail: "unknown");
            node.Marker = "?";
            node.AsciiMarker = "?";
            node.MarkerTone = ThemeToken.Muted;
            Review184Capture capture = new Review184Capture(tree, 60, GlyphTier.Ascii, ColorDepth.TrueColor);
            int x = capture.Raw.ToPlainText().Split('\n')[0].IndexOf('?');
            Assert.True(x >= 0);
            Assert.Equal(capture.Context.Style(ThemeToken.Muted).Color, capture.Raw[x, 0].Style.Color);
            Assert.Null(node.Status);
            Assert.DoesNotContain("SKIP", capture.Decoded);
            node.MarkerTone = null;
            Review184Capture defaults = new Review184Capture(tree, 60, GlyphTier.Ascii, ColorDepth.TrueColor);
            Assert.Equal(defaults.Context.Style(ThemeToken.Text).Color, defaults.Raw[x, 0].Style.Color);
        }

        [Theory]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 39, true)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 60, true)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 39, true)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60, true)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 39, false)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 60, false)]
        public void Render_OptInMiddleNote_KeepsOriginalNameAndDnsSuffixWithContextEllipsis(GlyphTier tier, ColorDepth depth, int width, bool columns) {
            Tree tree = new Tree { TruncateNotesInMiddle = true };
            if (columns) tree.ValueColumns.Add(new TableColumn("State") { MaxWidth = 25 });
            string detail = "cross-forest " + new string('d', 100) + ".corp.test";
            TreeNode node = tree.Add("Ops", StatusKind.Info, detail);
            node.Marker = "⇄";
            node.AsciiMarker = "<->";
            Review184Capture capture = new Review184Capture(tree, width, tier, depth);
            string raw = capture.Raw.ToPlainText();
            Assert.Contains("Ops  cross-forest", raw);
            Assert.Contains(".corp.test", raw);
            Assert.Contains("Ops  cross-forest", capture.Decoded);
            Assert.Contains(".corp.test", capture.Decoded);
            Assert.Contains(capture.Context.Ellipsis, raw);
            Assert.Equal(detail, node.Detail);
            Assert.False(tree.TruncateLabelsInMiddle);
            if (tier == GlyphTier.Ascii) {
                Assert.All(raw, character => Assert.InRange((int)character, 0, 127));
                Assert.All(capture.Encoded, character => Assert.InRange((int)character, 0, 127));
                Assert.Contains("...", raw);
            } else Assert.Contains("…", raw);
            tree.TruncateNotesInMiddle = false;
            string defaults = new Review184Capture(tree, width, tier, depth).Decoded;
            Assert.Contains("Ops  cross-forest", defaults);
            Assert.DoesNotContain(".corp.test", defaults);
        }

        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16)]
        [InlineData(GlyphTier.Basic, ColorDepth.None)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None)]
        public void Render_ShortIdentityAt39_RetainsBoundaryStateAndDomainBeforeOptionalValues(GlyphTier tier, ColorDepth depth) {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("Type") { MaxWidth = 10 });
            tree.ValueColumns.Add(new TableColumn("State") { MaxWidth = 25, Priority = 10 });
            tree.ValueColumns.Add(new TableColumn("Depth") { MaxWidth = 5, Priority = -1 });
            TreeNode node = tree.Add("Ops", StatusKind.Info, "cross-forest partner.test");
            node.Marker = "⇄";
            node.AsciiMarker = "<->";
            node.Values["Type"] = TableCell.FromValue("group");
            node.Values["State"] = TableCell.FromValue("cross-forest");
            node.Values["Depth"] = TableCell.FromValue(1);
            Review184Capture capture = new Review184Capture(tree, 39, tier, depth);
            string expected = (tier == GlyphTier.Ascii ? "<->" : "⇄") + " Ops  cross-forest partner.test";
            Assert.Contains(expected, capture.Raw.ToPlainText());
            Assert.Contains(expected, capture.Decoded);
            Assert.DoesNotContain("cross-forest  cross-forest", capture.Decoded);
        }

        [Fact]
        public void Render_ShortIdentityWithoutDetailAt39_RetainsHighPriorityStateBeforeLowerPriorityValues() {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("Type") { Priority = -1 });
            tree.ValueColumns.Add(new TableColumn("State") { MaxWidth = 25, Priority = 10 });
            TreeNode node = tree.Add("Ops", StatusKind.Info);
            node.Marker = "?";
            node.Values["Type"] = TableCell.FromValue(new string('t', 100));
            node.Values["State"] = TableCell.FromValue("unknown");
            string text = new Review184Capture(tree, 39, GlyphTier.Ascii, ColorDepth.None).Decoded;
            Assert.Contains("? Ops  unknown", text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None)]
        public void Render_LongStateAt120_ShrinksValuesBeforeDroppingTypeOrDepthAndReservesLabelShare(GlyphTier tier, ColorDepth depth) {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("Type") { MaxWidth = 10, Priority = 0 });
            tree.ValueColumns.Add(new TableColumn("State") { Priority = 10 });
            tree.ValueColumns.Add(new TableColumn("Depth") { MaxWidth = 5, Priority = -1 });
            TreeNode node = tree.Add("Original.Member.Name");
            node.Detail = new string('n', 160);
            node.Values["Type"] = TableCell.FromValue("account");
            node.Values["State"] = TableCell.FromValue(new string('s', 200));
            node.Values["Depth"] = TableCell.FromValue(3);
            Review184Capture capture = new Review184Capture(tree, 120, tier, depth);
            string[] lines = capture.Raw.ToPlainText().Split('\n');
            Assert.Contains("Type", lines[0]);
            Assert.Contains("Depth", lines[0]);
            Assert.Contains("Original.Member.Name", lines[1]);
            Assert.Contains("account", lines[1]);
            Assert.EndsWith("3", lines[1]);
            Assert.DoesNotContain("+2 columns", capture.Decoded);
            Assert.Contains(tier == GlyphTier.Ascii ? "..." : "…", lines[1]);
            Assert.True(lines[0].IndexOf("Type", StringComparison.Ordinal) >= 48);
        }

        [Fact]
        public void Render_AfterLabelWithOneCellState_RetainsCycleMarker() {
            Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
            tree.ValueColumns.Add(new TableColumn("State") { Width = 1, Priority = 10 });
            TreeNode node = tree.Add("CycleNode"); node.Reference = TreeReferenceKind.Cycle; node.Values["State"] = "value";
            Review184Capture capture = new Review184Capture(tree, 120, GlyphTier.Full, ColorDepth.None);
            Assert.Contains(capture.Context.Glyphs.Get(Glyph.Cycle), capture.Raw.ToPlainText());
        }

        [Theory]
        [InlineData(TreeReferenceKind.Cycle, "cycle")]
        [InlineData(TreeReferenceKind.Repeat, "shown above")]
        public void Render_LongBareReference_RetainsReferenceEvidence(TreeReferenceKind reference, string evidence) {
            Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
            tree.ValueColumns.Add(new TableColumn("State") { Priority = 10 });
            TreeNode ordinary = tree.Add("Short"); ordinary.Values["State"] = "healthy";
            TreeNode node = tree.Add(new string('L', 118)); node.Bare = true; node.Reference = reference; node.Values["State"] = "state";
            Review184Capture capture = new Review184Capture(tree, 120, GlyphTier.Full, ColorDepth.None);
            string text = capture.Raw.ToPlainText();
            Assert.Contains(reference == TreeReferenceKind.Cycle ? capture.Context.Glyphs.Get(Glyph.Cycle) : capture.Context.Glyphs.Get(Glyph.Repeat), text);
            Assert.Contains(evidence, text);
        }

        [Fact]
        public void Render_AfterLabelWithTruncatedIdentity_RetainsHiddenCycleMarker() {
            Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
            tree.ValueColumns.Add(new TableColumn("State") { Priority = 2 });
            tree.Add("CycleNode").Reference = TreeReferenceKind.Cycle;
            Review184Capture capture = new Review184Capture(tree, 60, GlyphTier.Full, ColorDepth.None);
            Assert.Contains(capture.Context.Glyphs.Get(Glyph.Cycle), capture.Raw.ToPlainText());
        }

        [Fact]
        public void Render_AfterLabelWithoutVisibleState_RetainsReferenceMarkerBeforeIdentity() {
            Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
            tree.ValueColumns.Add(new TableColumn("Type") { Priority = 10 });
            tree.ValueColumns.Add(new TableColumn("State") { Priority = 2 });
            TreeNode node = tree.Add("CycleNode"); node.Reference = TreeReferenceKind.Cycle; node.Values["Type"] = "group";
            Review184Capture capture = new Review184Capture(tree, 60, GlyphTier.Full, ColorDepth.None);
            string text = capture.Raw.ToPlainText();
            string glyph = capture.Context.Glyphs.Get(Glyph.Cycle);
            Assert.Equal(1, text.Split(new[] { glyph }, StringSplitOptions.None).Length - 1);
            Assert.Contains(glyph + " CycleNode", text);
            Assert.Contains("cycle", text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, 120)]
        [InlineData(GlyphTier.Full, 60)]
        [InlineData(GlyphTier.Ascii, 120)]
        [InlineData(GlyphTier.Ascii, 60)]
        public void Render_AfterLabelReferenceWithVisibleState_ShowsOneMarkerAfterIdentity(GlyphTier tier, int width) {
            Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
            tree.ValueColumns.Add(new TableColumn("State") { Priority = 10 });
            TreeNode cycle = tree.Add("CycleNode"); cycle.Reference = TreeReferenceKind.Cycle; cycle.Values["State"] = "original";
            TreeNode repeat = tree.Add("RepeatNode"); repeat.Reference = TreeReferenceKind.Repeat; repeat.Values["State"] = "original";
            Review184Capture capture = new Review184Capture(tree, width, tier, ColorDepth.None);
            string[] lines = capture.Raw.ToPlainText().Split('\n');
            string cycleLine = Assert.Single(lines, line => line.Contains("CycleNode"));
            string repeatLine = Assert.Single(lines, line => line.Contains("RepeatNode"));
            string cycleGlyph = capture.Context.Glyphs.Get(Glyph.Cycle);
            string repeatGlyph = capture.Context.Glyphs.Get(Glyph.Repeat);
            Assert.Equal(1, cycleLine.Split(new[] { cycleGlyph }, StringSplitOptions.None).Length - 1);
            Assert.Equal(1, repeatLine.Split(new[] { repeatGlyph }, StringSplitOptions.None).Length - 1);
            Assert.True(cycleLine.IndexOf("CycleNode", StringComparison.Ordinal) < cycleLine.IndexOf(cycleGlyph, StringComparison.Ordinal));
            Assert.True(repeatLine.IndexOf("RepeatNode", StringComparison.Ordinal) < repeatLine.IndexOf(repeatGlyph, StringComparison.Ordinal));
            Assert.Contains("cycle", cycleLine);
            Assert.Contains("shown above", repeatLine);
        }

        [Theory]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 60)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 39)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 39)]
        public void Render_NarrowReferencesAndLongNotes_KeepsGlyphThenOriginalLabelThenState(GlyphTier tier, ColorDepth depth, int width) {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("State") { MaxWidth = 25, Priority = 10 });
            TreeNode node = tree.Add("Original.Member", StatusKind.Info, new string('d', 150));
            node.Reference = TreeReferenceKind.Collapsed;
            node.ReferenceSummary = new string('r', 150);
            node.Values["State"] = TableCell.FromValue("cross-forest");
            Review184Capture capture = new Review184Capture(tree, width, tier, depth);
            string row = capture.Raw.ToPlainText().Split('\n')[0];
            string glyph = tier == GlyphTier.Full ? "▸" : ">";
            Assert.Contains(glyph + " Original.Member", row);
            Assert.True(row.IndexOf("Original.Member", StringComparison.Ordinal) < row.IndexOf("collapsed", StringComparison.Ordinal));
            Assert.Contains("Original.Member", capture.Decoded);
            Assert.DoesNotContain("rrr", row);
        }

        [Theory]
        [InlineData(GlyphTier.Full, "⇄")]
        [InlineData(GlyphTier.Basic, "⇄")]
        [InlineData(GlyphTier.Ascii, "<->")]
        public void Render_CustomBoundaryMarker_ReplacesStatusGlyphAndKeepsInfoToneWithoutFallbackOverrides(GlyphTier tier, string marker) {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("State"));
            tree.Add("Original.Member", StatusKind.Info).Marker = "⇄";
            tree.Root.Children[0].AsciiMarker = "<->";
            Review184Capture capture = new Review184Capture(tree, 60, tier, ColorDepth.Ansi16);
            Assert.Contains(marker + " Original.Member", capture.Raw.ToPlainText());
            Assert.Contains(marker + " Original.Member", capture.Decoded);
            int x = capture.Raw.ToPlainText().Split('\n')[0].IndexOf(marker, StringComparison.Ordinal);
            Assert.Equal(capture.Context.Style(ThemeToken.Info).Color, capture.Raw[x, 0].Style.Color);
        }

        [Fact]
        public void Render_OptInMiddleEllipsis_RetainsBothEndsOfLongIdentityBeforeNotes() {
            Tree tree = new Tree { TruncateLabelsInMiddle = true };
            tree.Add("original-member-in-a-very-long-forest.example.test", detail: new string('x', 100)).Reference = TreeReferenceKind.Incomplete;
            string text = new Review184Capture(tree, 39, GlyphTier.Basic, ColorDepth.None).Raw.ToPlainText();
            Assert.Contains("original", text);
            Assert.Contains(".example.test", text);
            Assert.Contains("…", text);
            Assert.DoesNotContain("xxx", text);
        }
    }
}
