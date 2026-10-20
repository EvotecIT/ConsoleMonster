using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
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
            table.AddValues("dc01.ad.adatum.com", "Warsaw-HQ", "Przemyslaw Klys", StatusKind.Pass, "primary domain controller");
            table.AddValues("dc02.ad.adatum.com", "Krakow-Branch", "Ada Lovelace", StatusKind.Fail, "replication lag");
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
            Assert.Contains("dc02.ad.adatum.com", text);
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
            table.AddValues(@"C:\ProgramData\DnsCache\A.json");
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
            Assert.Contains("Domains · 2", text);
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

        [Theory]
        [InlineData(TableBorder.Rounded)]
        [InlineData(TableBorder.Square)]
        [InlineData(TableBorder.Minimal)]
        public void Table_HeaderSeparator_IsFollowedDirectlyByTheFirstRow(TableBorder border) {
            Table table = Wide();
            table.Border = border;
            string[] lines = Render(table, 120).Replace("\r\n", "\n").Split('\n');
            int header = Array.FindIndex(lines, line => line.Contains("Host"));
            Assert.True(header >= 0);
            // Header, one separator row, then data: no blank padding row in between.
            Assert.Contains("dc01.ad.adatum.com", lines[header + 2]);
        }

        [Fact]
        public void Table_FirstGroupHeading_IsTheHeaderSeparator() {
            Table grouped = new Table { Border = TableBorder.Rounded };
            grouped.AddColumn(new TableColumn("Host") { IsKey = true });
            grouped.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grouped.AddGroup("Domain controllers");
            grouped.AddValues("dc01.example.test", StatusKind.Pass);
            string[] lines = Render(grouped, 80).Replace("\r\n", "\n").Split('\n');
            int header = Array.FindIndex(lines, line => line.Contains("Host"));
            Assert.StartsWith("├─ Domain controllers · 1 ", lines[header + 1]);
            Assert.EndsWith("┤", lines[header + 1].TrimEnd());
            Assert.Contains("dc01.example.test", lines[header + 2]);
        }

        [Theory]
        [InlineData(TableBorder.Rounded, "├─ Beta · 1 ", "┼")]
        [InlineData(TableBorder.Square, "├─ Beta · 1 ", "┼")]
        [InlineData(TableBorder.Double, "╠═ Beta · 1 ", "╬")]
        public void Table_GroupHeadings_AreLabelledRulesThatKeepTheColumnsJoined(TableBorder border, string heading, string cross) {
            Table grouped = new Table { Border = border, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Name") { IsKey = true });
            grouped.AddColumn(new TableColumn("Size"));
            grouped.AddColumn(new TableColumn("Kind"));
            grouped.AddGroup("Alpha");
            grouped.AddValues("a1", 1, "x");
            grouped.AddValues("a2", 2, "y");
            grouped.AddGroup("Beta");
            grouped.AddValues("b1", 3, "z");
            string[] lines = Render(grouped, 60).Replace("\r\n", "\n").Split('\n');
            string rule = Array.Find(lines, line => line.Contains("Beta"))!;
            Assert.StartsWith(heading, rule);
            Assert.Contains(cross, rule);
            // No line inside the table interrupts the column lines: every body line has the same width as the top rule.
            int width = lines[0].TrimEnd().Length;
            Assert.All(lines.Where(line => line.Length > 0 && !line.StartsWith(" ", StringComparison.Ordinal)), line => Assert.Equal(width, line.TrimEnd().Length));
        }

        [Fact]
        public void Table_GroupCutByTop_ShowsShownOfTotalAndTheGroupCount() {
            Table grouped = new Table { Border = TableBorder.Rounded, MaxRows = 3, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Name") { IsKey = true });
            grouped.AddColumn(new TableColumn("Size"));
            grouped.AddGroup("Alpha");
            grouped.AddValues("a1", 1);
            grouped.AddValues("a2", 2);
            grouped.AddGroup("Beta");
            grouped.AddValues("b1", 3);
            grouped.AddValues("b2", 4);
            grouped.AddValues("b3", 5);
            grouped.AddGroup("Gamma");
            grouped.AddValues("c1", 6);
            string text = Render(grouped, 60);
            Assert.Contains("Alpha · 2 ", text);
            Assert.Contains("Beta · 1 of 3 ", text);
            Assert.DoesNotContain("Gamma", text);
            Assert.Contains("Showing 3 of 6 · 2 of 3 groups", text);
        }

        [Fact]
        public void Table_GroupHeadingWiderThanTheFirstColumn_WidensItSoNoColumnLineIsCovered() {
            Table grouped = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Host"));
            grouped.AddColumn(new TableColumn("Name"));
            grouped.AddColumn(new TableColumn("Size"));
            grouped.AddGroup("Small");
            grouped.AddValues("dc01.example.test", "dc01", 3);
            grouped.AddGroup("Domain controllers");
            grouped.AddValues("dc02.example.test", "dc02", 4);
            string[] lines = Render(grouped, 80).Replace("\r\n", "\n").Split('\n');
            string row = Array.Find(lines, line => line.Contains("dc02"))!;
            foreach (string rule in lines.Where(line => line.Contains("Small") || line.Contains("Domain controllers"))) {
                for (int i = 0; i < row.Length; i++) {
                    if (row[i] == '│') {
                        Assert.True(i < rule.Length && "├┼┤".IndexOf(rule[i]) >= 0, $"column line at {i} is covered in '{rule}'");
                    }
                }
            }
        }

        [Fact]
        public void Table_GroupHeadingFarWiderThanANarrowFirstColumn_CrossesInsteadOfStretchingIt() {
            Table grouped = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Id"));
            grouped.AddColumn(new TableColumn("Name"));
            grouped.AddGroup("A rather long group heading");
            grouped.AddValues(1, "a");
            string[] lines = Render(grouped, 80).Replace("\r\n", "\n").Split('\n');
            string row = Array.Find(lines, line => line.Contains(" a "))!;
            // The Id column keeps its own width ("│ Id │"); the table is still wide enough for the whole heading.
            Assert.Equal(5, row.IndexOf('│', 1));
            Assert.Contains("A rather long group heading · 1", string.Join("\n", lines));
            Assert.EndsWith("┤", Array.Find(lines, line => line.Contains("A rather long"))!.TrimEnd());
        }

        [Fact]
        public void Table_GroupHeadings_DoNotWidenAColumnPastItsMaxWidth() {
            Table grouped = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Id") { MaxWidth = 4 });
            grouped.AddColumn(new TableColumn("Name"));
            grouped.AddGroup("Domain controllers");
            grouped.AddValues(1, "dc01");
            string[] lines = Render(grouped, 80).Replace("\r\n", "\n").Split('\n');
            string row = Array.Find(lines, line => line.Contains("dc01"))!;
            // "│ " + at most 4 + " │"
            Assert.InRange(row.IndexOf('│', 1), 1, 7);
            Assert.Contains("Domain controllers · 1", string.Join("\n", lines));
        }

        [Fact]
        public void Table_WithoutAHeader_FirstGroupHeadingIsTheTopRule() {
            Table grouped = new Table { Border = TableBorder.Rounded, ShowHeader = false, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Name") { IsKey = true });
            grouped.AddColumn(new TableColumn("Size"));
            grouped.AddGroup("Alpha");
            grouped.AddValues("a1", 1);
            grouped.AddGroup("Beta");
            grouped.AddValues("b1", 2);
            string[] lines = Render(grouped, 40).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            Assert.StartsWith("╭─ Alpha · 1 ", lines[0]);
            Assert.EndsWith("╮", lines[0].TrimEnd());
            Assert.Contains("a1", lines[1]);
            Assert.StartsWith("├─ Beta · 1 ", lines[2]);
            Assert.StartsWith("╰", lines[lines.Length - 1]);
            Assert.Equal(5, lines.Length);
        }

        [Fact]
        public void Table_WindowStartingOnAGroupHeading_DrawsItInPlaceOfTheHeaderSeparator() {
            Table grouped = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("Name") { IsKey = true });
            grouped.AddColumn(new TableColumn("Size"));
            grouped.AddGroup("Alpha");
            grouped.AddValues("a1", 1);
            grouped.AddGroup("Beta");
            grouped.AddValues("b1", 2);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(40, 20, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            // Rows: 0 Alpha, 1 a1, 2 Beta, 3 b1. A window from row 2 starts on the Beta heading.
            CellBuffer buffer = grouped.RenderWindow(40, 2, 2, context, out int headHeight, out List<int> rowLines, out _);
            string[] lines = buffer.ToPlainText().Replace("\r\n", "\n").Split('\n');
            Assert.Equal(2, headHeight);
            Assert.Contains("Name", lines[1]);
            Assert.StartsWith("├─ Beta · 1 ", lines[2]);
            Assert.Contains("b1", lines[3]);
            Assert.Equal(new List<int> { 2, 3 }, rowLines);
        }

        [Fact]
        public void Table_TooNarrowForAGroupLabel_KeepsTheRuleAndItsBorder() {
            Table grouped = new Table { Border = TableBorder.Rounded, RecordViewRows = 0 };
            grouped.AddColumn(new TableColumn("N"));
            grouped.AddGroup("Alpha");
            grouped.AddValues("a");
            string[] lines = Render(grouped, 5).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            string rule = lines[2];
            Assert.StartsWith("├", rule);
            Assert.EndsWith("┤", rule.TrimEnd());
            Assert.Equal(lines[0].TrimEnd().Length, rule.TrimEnd().Length);
        }

        [Fact]
        public void Table_SingleGroupedRow_RecordViewKeepsTheGroupHeading() {
            Table grouped = new Table { Border = TableBorder.Rounded };
            grouped.AddColumn(new TableColumn("Id"));
            grouped.AddColumn(new TableColumn("Cpu"));
            grouped.AddColumn(new TableColumn("Handles"));
            grouped.AddGroup("pwsh");
            grouped.AddValues(4242, 1.5, 300);
            string[] lines = Render(grouped, 60).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            Assert.Contains(lines, line => line.Trim().StartsWith("pwsh", StringComparison.Ordinal) && line.Contains("1"));
            Assert.Contains(lines, line => line.Contains("Handles"));
            Assert.DoesNotContain(lines, line => line.Contains("record"));
        }

        [Fact]
        public void Table_MinimalAndNoneBorders_ShowGroupHeadings() {
            foreach (TableBorder border in new[] { TableBorder.Minimal, TableBorder.None }) {
                Table grouped = new Table { Border = border, RecordViewRows = 0 };
                grouped.AddColumn(new TableColumn("Name") { IsKey = true });
                grouped.AddColumn(new TableColumn("Size"));
                grouped.AddGroup("Alpha");
                grouped.AddValues("a1", 1);
                Assert.Contains("Alpha · 1", Render(grouped, 40));
            }
        }
    }
}
