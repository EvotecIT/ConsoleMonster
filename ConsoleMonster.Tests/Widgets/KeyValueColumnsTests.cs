using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class KeyValueColumnsTests {
        [Theory]
        [InlineData(80, HorizontalAlignment.Right, 9)]
        [InlineData(48, HorizontalAlignment.Right, 9)]
        [InlineData(80, HorizontalAlignment.Center, 5)]
        [InlineData(48, HorizontalAlignment.Center, 5)]
        public void NumericColumnSettingsSurviveMultipleColumnsAndNarrowFallback(int width, HorizontalAlignment alignment, int spaces) {
            var facts = new KeyValueList {
                Columns = 2,
                NumberColumn = new ColumnFormat(alignment, minWidth: 4, reserveDigits: 8)
            }.Add("A", "1").Add("B", "2");
            string text = Terminal.RenderText(facts, TerminalProfile.Fixed(width, 20, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Contains("A" + new string(' ', spaces) + "1", text);
            Assert.Contains("B" + new string(' ', spaces) + "2", text);
        }
    }
}
