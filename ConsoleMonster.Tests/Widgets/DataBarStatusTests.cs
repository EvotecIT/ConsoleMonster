using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class DataBarStatusTests {
        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor, 120)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 120)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi256, 60)]
        [InlineData(GlyphTier.Basic, ColorDepth.TrueColor, 60)]
        [InlineData(GlyphTier.Ascii, ColorDepth.Ansi16, 60)]
        [InlineData(GlyphTier.Full, ColorDepth.None, 120)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 60)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 120)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 120)]
        public void Render_ThresholdDataBars_AlwaysKeepValueThenStatusInRawAndEncodedOutput(GlyphTier tier, ColorDepth depth, int width) {
            Table table = new Table { Border = TableBorder.None, ShowHeader = false, RecordViewRows = 0 };
            table.AddColumn("Volume");
            TableColumn used = new TableColumn("Used") { Presenter = new DataBarPresenter(100, barWidth: 8) };
            used.RoleRules.Add(RoleRule.Above(93, ThemeToken.Fail));
            used.RoleRules.Add(RoleRule.Above(90, ThemeToken.Warn));
            table.AddColumn(used);
            table.AddValues("volume-a", 92);
            table.AddValues("volume-b", 96);
            Review184Capture capture = new Review184Capture(table, width, tier, depth);
            string warn = "92 " + capture.Context.Glyphs.StatusSymbol(StatusKind.Warn);
            string fail = "96 " + capture.Context.Glyphs.StatusSymbol(StatusKind.Fail);
            Assert.Contains(warn, capture.Raw.ToPlainText());
            Assert.Contains(fail, capture.Raw.ToPlainText());
            Assert.Contains(warn, capture.Decoded);
            Assert.Contains(fail, capture.Decoded);
            int value = capture.Raw.ToPlainText().Split('\n')[0].IndexOf(warn, StringComparison.Ordinal);
            Assert.Equal(capture.Context.Style(ThemeToken.Warn).Color, capture.Raw[value + 3, 0].Style.Color);
            if (depth == ColorDepth.None) Assert.DoesNotContain("\u001b", capture.Encoded, StringComparison.Ordinal);
            else Assert.Contains("\u001b[", capture.Encoded, StringComparison.Ordinal);
        }

        [Fact]
        public void WithTone_VendorSeverity_LeavesRawNumberAndOriginalCellIntactAndTonesTheBar() {
            TableCell original = TableCell.FromValue(1.1m);
            TableCell critical = original.WithTone(ThemeToken.Fail);
            Assert.NotSame(original, critical);
            Assert.Same(original.Value, critical.Value);
            Assert.Equal(1.1m, critical.Value);
            Assert.Null(original.RowRole);
            Table table = new Table { Border = TableBorder.None, ShowHeader = false, RecordViewRows = 0 };
            table.AddColumn("Item");
            table.AddColumn(new TableColumn("CVSS") { Presenter = new DataBarPresenter(10, barWidth: 8) });
            table.AddRow("critical", critical);
            Review184Capture capture = new Review184Capture(table, 120, GlyphTier.Basic, ColorDepth.Ansi16);
            CellContent content = CellRendering.Content(critical, table.Columns[1].Presenter, null, new PresenterContext(capture.Context));
            Assert.Equal(ThemeToken.Fail, content.Presented!.Role);
            Assert.Equal(1.1, content.Presented.Number!.Value, 4);
            Assert.Contains("1.1 " + capture.Context.Glyphs.StatusSymbol(StatusKind.Fail), capture.Decoded);
            int firstBar = capture.Raw.ToPlainText().IndexOf(capture.Context.Glyphs.Get(Glyph.HalfLower), StringComparison.Ordinal);
            Assert.True(firstBar >= 0);
            Assert.Equal(capture.Context.Style(ThemeToken.Fail).Color, capture.Raw[firstBar, 0].Style.Color);
        }
    }
}
