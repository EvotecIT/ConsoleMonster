using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Long tables print in blocks with the same result as one block, keeping column widths stable.</summary>
    public sealed class TableStreamingTests {
        private static Table Large(int rows) {
            Table table = new Table { Title = "Files", Caption = "invented data", ShowTotals = true };
            table.AddColumn("Name");
            table.AddColumn(new TableColumn("Size") { Aggregate = ColumnAggregate.Sum });
            table.AddColumn("Note");
            for (int i = 0; i < rows; i++) {
                if (i % 1000 == 0) {
                    table.AddGroup("Batch " + (i / 1000).ToString(CultureInfo.InvariantCulture));
                }
                // A few long notes wrap, so rows have different heights and widths come from late rows too.
                string note = i == rows - 3 ? "a much longer note that appears only near the end of the table and wraps" : "ok";
                table.AddValues("file-" + i.ToString("00000", CultureInfo.InvariantCulture), i, note);
            }
            return table;
        }

        private static string Text(IEnumerable<CellBuffer> blocks) {
            StringBuilder sb = new StringBuilder();
            foreach (CellBuffer block in blocks) {
                if (sb.Length > 0) {
                    sb.Append('\n');
                }
                sb.Append(block.ToPlainText());
            }
            return sb.ToString();
        }

        [Fact]
        public void RenderChunks_LongTable_MatchesOneBlock() {
            Table table = Large(2600);
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(70, 30, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.InvariantCulture), Theme.Dark);
            List<CellBuffer> blocks = new List<CellBuffer>(Terminal.RenderChunks(table, 70, context, 300));
            Assert.True(blocks.Count > 5, "printed in blocks");
            Assert.Equal(FrameComposer.RenderBlock(table, 70, context).ToPlainText(), Text(blocks));
        }

        [Fact]
        public void RenderChunks_ShortTableOrOtherWidget_IsOneBlock() {
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(70, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Single(Terminal.RenderChunks(Large(50), 70, context));
            Assert.Single(Terminal.RenderChunks(new TextBlock("x"), 70, context));
        }
    }
}
