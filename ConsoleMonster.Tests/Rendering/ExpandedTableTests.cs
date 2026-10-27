using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// An expanded table shares its spare width: free-text columns grow in proportion to their widths, while numbers, dates and
    /// status words keep their natural width; Weight and Flexible columns take the spare width when set, and fixed widths hold.
    /// </summary>
    public sealed class ExpandedTableTests {
        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

        [Theory]
        [InlineData(100)]
        [InlineData(120)]
        public void MixedColumns_Expanded_MatchGolden(int width) {
            string text = Render(Fleet(), width);
            Assert.All(text.Split('\n').Where(line => line.Length > 0), line => Assert.Equal(width, line.Length));
            Golden.Match("tables.expanded.mixed." + width.ToString(CultureInfo.InvariantCulture) + ".txt", text);
        }

        [Fact]
        public void NumbersDatesAndStatusWordsKeepTheirNaturalWidth() {
            int[] narrow = Widths(Render(Fleet(), 90));
            int[] wide = Widths(Render(Fleet(), 140));
            // Host, State, Missing, Patched, Notes: only the text columns grow.
            Assert.Equal(narrow[1], wide[1]);
            Assert.Equal(narrow[2], wide[2]);
            Assert.Equal(narrow[3], wide[3]);
            Assert.True(wide[0] > narrow[0]);
            Assert.True(wide[4] > narrow[4]);
            Assert.True(wide[4] > wide[0], "the wider text column takes the larger share");
        }

        [Fact]
        public void WeightedColumnsTakeTheSpareWidthByWeight() {
            Table table = Fleet();
            table.Columns[0].Weight = 1;
            table.Columns[1].Weight = 3;
            int[] widths = Widths(Render(table, 110));
            int[] natural = Widths(Render(Natural(), 110));
            Assert.Equal(natural[4], widths[4]);
            int hostExtra = widths[0] - natural[0];
            int stateExtra = widths[1] - natural[1];
            Assert.True(stateExtra >= 3 * hostExtra - 3 && stateExtra <= 3 * hostExtra + 3, $"host +{hostExtra}, state +{stateExtra}");
        }

        [Fact]
        public void FixedAndMaximumWidthsHoldWhileTheRestGrow() {
            Table table = Fleet();
            table.Columns[0].Width = 10;
            table.Columns[4].MaxWidth = 30;
            int[] widths = Widths(Render(table, 120));
            Assert.Equal(10, widths[0]);
            Assert.Equal(30, widths[4]);
            Assert.Equal(120, Render(table, 120).Split('\n')[0].Length);
        }

        [Fact]
        public void TableWithoutTextGrowsEveryColumn() {
            Table table = new Table { Expand = true };
            table.AddColumn("Cpu").AddColumn("Memory");
            table.AddRow(TableCell.FromValue(12), TableCell.FromValue(64));
            string text = Render(table, 40);
            Assert.Equal(40, text.Split('\n')[0].Length);
        }

        private static Table Fleet() {
            Table table = new Table { Expand = true };
            Configure(table);
            return table;
        }

        private static Table Natural() {
            Table table = new Table();
            Configure(table);
            return table;
        }

        private static void Configure(Table table) {
            table.AddColumn("Host").AddColumn("State").AddColumn("Missing").AddColumn("Patched").AddColumn("Notes");
            table.AddRow(TableCell.FromValue("dc-01"), TableCell.ForStatus(StatusKind.Pass, "Compliant"), TableCell.FromValue(0), TableCell.FromValue(new DateTime(2026, 10, 15)), TableCell.FromValue("Patched in the pilot ring"));
            table.AddRow(TableCell.FromValue("sql-01"), TableCell.ForStatus(StatusKind.Fail, "Failed"), TableCell.FromValue(3), TableCell.FromValue(new DateTime(2026, 9, 12)), TableCell.FromValue("Missing the cumulative update and the servicing stack"));
            table.AddRow(TableCell.FromValue("print-01"), TableCell.ForStatus(StatusKind.Warn, "Pending"), TableCell.FromValue(2), TableCell.FromValue(new DateTime(2026, 10, 2)), TableCell.FromValue("Reboot scheduled"));
        }

        private static string Render(Table table, int width) =>
            Terminal.RenderText(table, TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Full).WithCulture(EnUs), Theme.Dark);

        // Column widths from the top border: the cells between ┬ separators, without the one-cell padding on each side.
        private static int[] Widths(string text) {
            string top = text.Split('\n')[0];
            return top.Substring(1, top.Length - 2).Split('┬').Select(part => part.Length - 2).ToArray();
        }
    }
}
