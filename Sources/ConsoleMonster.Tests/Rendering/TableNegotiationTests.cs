using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Width negotiation, overflow, stable widths, cards, record views, groups, totals and top-N.</summary>
    public sealed class TableNegotiationTests {
        private static string Render(Table table, int width, GlyphTier tier = GlyphTier.Full) {
            return TableShowcaseGoldenTests.WithInvariantCulture(() => Terminal.RenderText(table, TerminalProfile.Fixed(width, 60, ColorDepth.None, tier), Theme.Dark));
        }

        private static Table Wide() {
            Table table = new Table { Border = TableBorder.Rounded };
            table.AddColumn(new TableColumn("Host") { IsKey = true });
            table.AddColumn(new TableColumn("Site") { Priority = 1 });
            table.AddColumn(new TableColumn("Owner") { Priority = 0 });
            table.AddColumn(new TableColumn("Status") { Priority = 9, Presenter = CellPresenters.Status });
            table.AddColumn(new TableColumn("Notes") { Priority = 0 });
            table.AddValues("dc01.ad.evotec.xyz", "Warsaw-HQ", "Przemyslaw Klys", StatusKind.Pass, "primary domain controller");
            table.AddValues("dc02.ad.evotec.xyz", "Krakow-Branch", "Ada Lovelace", StatusKind.Fail, "replication lag");
            return table;
        }

        [Fact]
        public void Negotiation_TooNarrow_HidesLowestPriorityRightmostFirstAndNamesThem() {
            Table table = Wide();
            table.CardsBelow = 0;
            Assert.Contains("+1 column (Notes)", Render(table, 64));
            string text = Render(table, 56);
            Assert.Contains("+2 columns (Owner, Notes)", text);
            Assert.Contains("Site", text);
            Assert.Contains("Status", text);
            Assert.True(text.Split('\n').All(line => line.Length <= 56));
        }

        [Fact]
        public void Negotiation_KeyColumn_NeverHides() {
            Table table = Wide();
            table.CardsBelow = 0;
            string text = Render(table, 30);
            Assert.Contains("Host", text);
            Assert.Contains("dc01", text);
        }

        [Fact]
        public void Negotiation_BelowCardThreshold_RendersRecordCards() {
            string text = Render(Wide(), 40);
            Assert.Contains("2 records", text);
            Assert.Contains("dc02.ad.evotec.xyz", text);
            Assert.Contains("Owner", text);
            Assert.Contains("Ada Lovelace", text);
            Assert.DoesNotContain("columns (", text);
        }

        [Fact]
        public void Negotiation_SingleRow_RendersRecordView() {
            Table table = new Table { Title = "dc01" };
            table.AddColumn("Host").AddColumn("Site").AddColumn("Owner");
            table.AddValues("dc01", "Warsaw", null);
            string text = Render(table, 60);
            Assert.Contains("Site", text);
            Assert.Contains("Warsaw", text);
            Assert.DoesNotContain("Owner", text);
            Assert.DoesNotContain("╭", text);
        }

        [Theory]
        [InlineData(ColumnOverflow.Truncate, "C:\\ProgramData\\DnsC…")]
        [InlineData(ColumnOverflow.TruncateMiddle, "…\\cache\\A.json")]
        public void Overflow_TruncateModes_KeepTheExpectedEnd(ColumnOverflow overflow, string expected) {
            Table table = new Table { Border = TableBorder.None, RecordViewRows = 0, CardsBelow = 0 };
            table.AddColumn(new TableColumn("Path") { Overflow = overflow, Width = 20 });
            table.AddValues(@"C:\ProgramData\DnsClientX\cache\A.json");
            string text = Render(table, 40);
            Assert.Contains(overflow == ColumnOverflow.Truncate ? expected : "A.json", text);
            Assert.Contains("…", text);
        }

        [Fact]
        public void Overflow_WrapWithMaxLines_EndsWithEllipsis() {
            Table table = new Table { Border = TableBorder.None, RecordViewRows = 0, CardsBelow = 0 };
            table.AddColumn(new TableColumn("Data") { Overflow = ColumnOverflow.Wrap, MaxLines = 2, Width = 12 });
            table.AddValues("v=spf1 include:spf.protection.outlook.com include:_spf.google.com -all");
            string[] lines = Render(table, 30).Split('\n').Where(l => l.Trim().Length > 0).ToArray();
            Assert.Equal(3, lines.Length);
            Assert.EndsWith("…", lines[2].TrimEnd());
        }

        [Fact]
        public void StableWidths_LiveValuesShrink_ColumnsKeepTheirWidth() {
            Table table = new Table { Border = TableBorder.None, RecordViewRows = 0 };
            table.AddColumn("Name").AddColumn("Detail");
            table.AddValues("a", "a fairly long detail value");
            table.AddValues("b", "x");
            int before = Render(table, 80).Split('\n')[0].TrimEnd().Length;
            Table again = table;
            again.Rows[0][1] = TableCell.FromValue("short");
            int after = Render(again, 80).Split('\n')[0].TrimEnd().Length;
            Assert.Equal(before, after);
        }

        [Fact]
        public void Groups_SubtotalsAndTotals_SumPerGroupAndOverall() {
            Table table = new Table { Border = TableBorder.None, ShowSubtotals = true, ShowTotals = true, RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Target") { IsKey = true });
            table.AddColumn(new TableColumn("Failed") { Aggregate = ColumnAggregate.Sum, Presenter = CellPresenters.Number });
            table.AddColumn(new TableColumn("Took") { Aggregate = ColumnAggregate.Sum, Presenter = CellPresenters.Duration });
            table.AddGroup("Domains");
            table.AddValues("ad", 2, TimeSpan.FromSeconds(10));
            table.AddValues("eu", 1, TimeSpan.FromSeconds(20));
            table.AddGroup("Controllers");
            table.AddValues("dc01", 4, TimeSpan.FromSeconds(30));
            string text = Render(table, 60);
            Assert.Contains("Domains  2", text);
            Assert.Matches(@"Subtotal\s+3\s+30 s", text);
            Assert.Matches(@"Subtotal\s+4\s+30 s", text);
            Assert.Matches(@"Total\s+7\s+1 m 00 s", text);
        }

        [Fact]
        public void TopN_MaxRowsAndTotalRowCount_ShowsTheShowingFooter() {
            Table table = new Table { Border = TableBorder.None, MaxRows = 2, TotalRowCount = 1234, RecordViewRows = 0 };
            table.AddColumn("Name");
            table.AddValues("a");
            table.AddValues("b");
            table.AddValues("c");
            string text = Render(table, 40);
            Assert.Contains("Showing 2 of 1,234", text);
            Assert.DoesNotContain(" c", text);
        }

        [Fact]
        public void DataBar_ScalesToColumnMaximumAndKeepsTheNumber() {
            Table table = new Table { Border = TableBorder.None, RecordViewRows = 0 };
            table.AddColumn("Name").AddColumn(new TableColumn("Score") { Presenter = CellPresenters.DataBar });
            table.AddValues("full", 100);
            table.AddValues("half", 50);
            string[] lines = Render(table, 40).Split('\n');
            int full = lines[1].Count(ch => ch == '▆');
            int half = lines[2].Count(ch => ch == '▆');
            Assert.True(full > half && half > 0, full + " vs " + half);
            Assert.Contains("100", lines[1]);
        }
    }
}
