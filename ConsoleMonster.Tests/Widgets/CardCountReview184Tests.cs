using ConsoleMonster.Capabilities;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CardCountReview184Tests {
        [Fact]
        public void Render_OptOutCardCount_KeepsTitleAndCaptionWithoutRedundantRecordCount() {
            Table table = Cards();
            Assert.Contains("2 records", new Review184Capture(table, 60, GlyphTier.Basic, ColorDepth.None).Decoded);
            table.ShowCardCount = false;
            string text = new Review184Capture(table, 60, GlyphTier.Basic, ColorDepth.None).Decoded;
            Assert.Contains("Summary", text);
            Assert.Contains("caller caption", text);
            Assert.DoesNotContain("2 records", text);
        }

        [Fact]
        public void Render_OptOutWithOmittedRows_StillDisclosesIncompleteOutput() {
            Table table = Cards();
            table.ShowCardCount = false;
            table.MaxRows = 1;
            Assert.Contains("Showing 1 of 2", new Review184Capture(table, 60, GlyphTier.Ascii, ColorDepth.None).Decoded);
        }

        private static Table Cards() {
            Table table = new Table { Title = "Summary", Caption = "caller caption", AlwaysCards = true };
            table.AddColumn("Name").AddColumn("State");
            table.AddRow("one", "ready").AddRow("two", "ready");
            return table;
        }
    }
}
