using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class RoleTableTests {
        [Theory]
        [InlineData(GlyphTier.Full, "★")]
        [InlineData(GlyphTier.Ascii, "*")]
        public void Render_RowRole_HasADedicatedMarkerColumnAndPreservesNumericTotals(GlyphTier tier, string marker) {
            TableColumn name = new TableColumn("Name");
            RoleTable rows = new RoleTable(new[] { name, new TableColumn("Count") { Aggregate = ColumnAggregate.Sum } });
            rows.Table.ShowTotals = true;
            rows.Table.RecordViewRows = 0;
            rows.AddRow(new TableRowRole { Bold = true, Marker = "★", AsciiMarker = "*" }, "first", TableCell.FromValue(7));
            rows.AddRow(new TableRowRole { Muted = true }, "second", TableCell.FromValue(11));
            string text = TestTerminal.RenderText(rows, 120, glyphs: tier);
            Assert.Contains(marker, text);
            Assert.Contains("first", text);
            Assert.Contains("second", text);
            Assert.Contains("18", text);
            Assert.Equal(3, rows.Table.Columns.Count);
            Assert.Equal(7, rows.Table.Rows[0][2].Value);
            Assert.False(name.IsKey);
        }

        [Fact]
        public void Render_WholeRowToneAndBold_AffectEachCellWithoutChangingInputCells() {
            TableCell source = TableCell.FromValue(7);
            Table table = new Table { Border = TableBorder.None, ShowHeader = false, RecordViewRows = 0 };
            table.AddColumn("Name").AddColumn("Count");
            table.AddRow(new TableRowRole { Muted = true, Bold = true }, "entry", source);
            RenderContext context = Terminal.CreateContext(TestTerminal.Profile(80, colors: ColorDepth.Ansi16), Theme.Dark);
            CellBuffer frame = FrameComposer.RenderBlock(table, 80, context);
            string text = frame.ToPlainText();
            int nameX = text.IndexOf("entry", StringComparison.Ordinal);
            int numberX = text.IndexOf('7');
            Assert.True(nameX >= 0 && numberX > nameX);
            Assert.Equal(context.Style(ThemeToken.Muted).Color, frame[nameX, 0].Style.Color);
            Assert.Equal(context.Style(ThemeToken.Muted).Color, frame[numberX, 0].Style.Color);
            Assert.True((frame[nameX, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.True((frame[numberX, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.NotSame(source, table.Rows[0][1]);
            Assert.Equal(7, source.Value);
        }

        [Fact]
        public void Render_Cards_KeepsMarkerAndEveryDataValue() {
            RoleTable rows = new RoleTable(new[] { new TableColumn("Name"), new TableColumn("Count"), new TableColumn("Detail") });
            rows.Table.AlwaysCards = true;
            rows.AddRow(new TableRowRole { Marker = "★", AsciiMarker = "*", Bold = true }, "entry", TableCell.FromValue(9), "evidence");
            string text = TestTerminal.RenderText(rows, 40, glyphs: GlyphTier.Ascii);
            Assert.Contains("*", text);
            Assert.Contains("entry", text);
            Assert.Contains("9", text);
            Assert.Contains("evidence", text);
        }
    }
}
