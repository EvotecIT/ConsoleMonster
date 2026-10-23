using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class ColumnBandRegressionTests {
        private static Table Bands(int rows = 3, bool grouped = false) {
            Table table = new Table { KeepAllColumns = true, RecordViewRows = 0, WrapHeaders = true, Title = "Inventory", Caption = "End of inventory" };
            table.AddColumn(new TableColumn("Key") { IsKey = true });
            table.AddColumn("First observation");
            table.AddColumn("Second observation");
            table.AddColumn("Third observation");
            table.GroupHeadersAsRows = grouped;
            for (int i = 0; i < rows; i++) {
                if (grouped && i % 80 == 0) table.AddGroup("Group " + i);
                table.AddValues("n" + i, "a" + i, "b" + i, "c" + i);
            }
            return table;
        }

        private static RenderContext Context(int width) => Terminal.CreateContext(
            TerminalProfile.Fixed(width, 4000, ColorDepth.None, GlyphTier.Full), Theme.Dark);

        [Theory]
        [InlineData(26)]
        [InlineData(120)]
        public void ColumnBands_CaptionFollowsEveryGrid(int width) {
            string text = FrameComposer.RenderBlock(Bands(), width, Context(width)).ToPlainText();
            Assert.Equal("End of inventory", text.Split('\n').Last().Trim());
            Assert.Equal(1, text.Split(new[] { "End of inventory" }, StringSplitOptions.None).Length - 1);
        }

        [Theory]
        [InlineData(26, 1)]
        [InlineData(120, 1)]
        [InlineData(26, 3)]
        [InlineData(120, 3)]
        public void ColumnBands_ExplicitCardsWinAtEveryWidth(int width, int rows) {
            Table table = Bands(rows);
            table.AlwaysCards = true;
            table.RecordViewRows = 1;
            string text = FrameComposer.RenderBlock(table, width, Context(width)).ToPlainText();
            table.KeepAllColumns = false;
            table.RecordViewRows = 0;
            string cards = FrameComposer.RenderBlock(table, width, Context(width)).ToPlainText();
            Assert.Equal(cards, text);
            Assert.Contains(rows == 1 ? "1 record" : "3 records", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ColumnBands_StreamInBoundedBuffersWithIdenticalOutput(bool grouped) {
            const int width = 26;
            const int limit = 8;
            Table table = Bands(240, grouped);
            RenderContext context = Context(width);
            CellBuffer[] chunks = table.RenderChunks(width, context, limit).ToArray();
            Assert.True(chunks.Length > 30);
            Assert.All(chunks, chunk => Assert.InRange(chunk.Height, 1, limit));
            string streamed = string.Join("\n", chunks.Select(chunk => chunk.ToPlainText()));
            Assert.Equal(FrameComposer.RenderBlock(table, width, context).ToPlainText(), streamed);
            Assert.Contains("c239", streamed);
            Assert.EndsWith("End of inventory", streamed);
        }
    }
}
