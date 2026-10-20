using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Numbers in a column share the decimals the column needs (at most two), so the separators line up.</summary>
    public sealed class ColumnDecimalsTests {
        private static string[] Render(Table table, string culture = "en-US") {
            return TableShowcaseGoldenTests.WithInvariantCulture(() => {
                table.Presentation = new PresentationOptions { Culture = System.Globalization.CultureInfo.GetCultureInfo(culture) };
                return Terminal.RenderText(table, TerminalProfile.Fixed(60, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            }).Replace("\r\n", "\n").Split('\n');
        }

        private static Table Numbers(params object[] values) {
            Table table = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Name"));
            table.AddColumn(new TableColumn("Value"));
            for (int i = 0; i < values.Length; i++) {
                table.AddValues("row" + i, values[i]);
            }
            return table;
        }

        private static string Cell(string[] lines, string row) {
            string line = lines.First(l => l.Contains(row + " "));
            return line.Split('│')[2].Trim();
        }

        [Fact]
        public void Column_OneDecimalNeeded_EveryNumberShowsOne() {
            string[] lines = Render(Numbers(7.0, 11.7, 344.4));
            Assert.Equal("7.0", Cell(lines, "row0"));
            Assert.Equal("11.7", Cell(lines, "row1"));
            Assert.Equal("344.4", Cell(lines, "row2"));
        }

        [Fact]
        public void Column_DecimalsAreCappedAtTwo_AndFollowTheCulture() {
            string[] lines = Render(Numbers(3.14159, 2.0, 1234.5), "pl-PL");
            Assert.Equal("3,14", Cell(lines, "row0"));
            Assert.Equal("2,00", Cell(lines, "row1"));
            Assert.Equal("1 234,50", Cell(lines, "row2"));
        }

        [Fact]
        public void Column_WholeNumbersAndIntegers_ShowNoDecimals() {
            string[] lines = Render(Numbers(12.0, 3, 1500L));
            Assert.Equal("12", Cell(lines, "row0"));
            Assert.Equal("3", Cell(lines, "row1"));
            Assert.Equal("1,500", Cell(lines, "row2"));
        }

        [Fact]
        public void Column_SeparatorsLineUp() {
            string[] lines = Render(Numbers(0.5, 120.25, 9.0));
            int[] positions = new[] { "row0", "row1", "row2" }.Select(row => lines.First(l => l.Contains(row + " ")).IndexOf('.')).ToArray();
            Assert.All(positions, position => Assert.Equal(positions[0], position));
        }

        [Fact]
        public void Column_HugeNumbers_RenderWithoutDecimals() {
            // Larger than a decimal can hold: no crash, and the other values keep the column's decimals.
            Assert.Contains(Render(Numbers(double.MaxValue, float.MaxValue)), line => line.Contains("row1"));
            string[] lines = Render(Numbers(1e20, 2.5));
            Assert.Equal("2.5", Cell(lines, "row1"));
            Assert.EndsWith(".0", Cell(lines, "row0"));
        }

        [Fact]
        public void Column_NumericText_KeepsItsDecimals() {
            Table table = Numbers("1.5", "2.75", "3");
            table.Columns[1].Presenter = new NumberPresenter();
            string[] lines = Render(table);
            Assert.Equal("1.50", Cell(lines, "row0"));
            Assert.Equal("2.75", Cell(lines, "row1"));
            Assert.Equal("3.00", Cell(lines, "row2"));
        }

        [Theory]
        [InlineData(1e30, 0)]
        [InlineData("1.5", 1)]
        [InlineData("2", 0)]
        [InlineData(7.0, 0)]
        [InlineData(11.7, 1)]
        [InlineData(3.14159, 2)]
        [InlineData(2.996, 0)]
        [InlineData(5, 0)]
        public void DecimalsNeeded_RoundsToTwoFirst(object value, int expected) {
            Assert.Equal(expected, PresenterContext.DecimalsNeeded(value));
        }
    }
}
