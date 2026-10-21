using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>The table sizing rules from the report quality bar, applied to console tables.</summary>
    public sealed class TableContractTests {
        [Fact]
        public void KeepAllColumns_LongKeysAtFortyColumns_PreservesTheOnlyValueColumn() {
            Table table = new Table { KeepAllColumns = true, WrapHeaders = true, RecordViewRows = 0, WrapLines = 0 };
            table.AddColumn(new TableColumn("CA") { IsKey = true, MaxWidth = 20 });
            table.AddColumn(new TableColumn("Issued") { MaxWidth = 7 });
            table.AddColumn(new TableColumn("UserProtected off") { MaxWidth = 17 });
            table.AddValues("ca01.example.test\\Issuing CA", 42, "Enabled");
            table.AddValues("ca02.example.test\\Issuing CA", 24, "Disabled");

            string text = Render(table, 40);
            Assert.Contains("UserProtected", text);
            Assert.Contains("Enabled", text);
            Assert.Contains("Disabled", text);
            Assert.DoesNotContain("columns (", text);
        }

        [Theory]
        [InlineData(45)]
        [InlineData(120)]
        public void KeepAllColumns_NarrowGrid_RepeatsKeysAndKeepsEveryField(int width) {
            Table table = new Table { KeepAllColumns = true, WrapHeaders = true, RecordViewRows = 0, WrapLines = 0 };
            table.AddColumn(new TableColumn("Authority") { IsKey = true, MaxWidth = 12 });
            table.AddColumn(new TableColumn("Issued") { MaxWidth = 8 });
            table.AddColumn(new TableColumn("SAN from request") { MaxWidth = 10 });
            table.AddColumn(new TableColumn("UserProtected off") { MaxWidth = 10 });
            table.AddColumn(new TableColumn("Mapping methods") { MaxWidth = 10 });
            table.AddValues("CA[1]", 42, "SAN enabled", "Protected", "Explicit");
            table.AddValues("CA[2]", 24, "SAN disabled", "Unprotected", "Implicit");

            string text = Render(table, width);
            Assert.Contains("CA[1]", text);
            Assert.Contains("CA[2]", text);
            Assert.Contains("42", text);
            Assert.Contains("24", text);
            Assert.Contains("enabled", text);
            Assert.Contains("disabled", text);
            Assert.Contains("Protected", text);
            Assert.Contains("Explicit", text);
            Assert.Contains("Implicit", text);
            Assert.DoesNotContain("+1 column", text);
            Assert.DoesNotContain("record", text);
        }

        private static string Render(Table table, int width) {
            return Terminal.RenderText(table, TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Full), Theme.Dark);
        }

        /// <summary>Section groups break inner borders cleanly and keep their muted counts clear of column positions.</summary>
        [Theory]
        [InlineData(60, false, TableBorder.Rounded)]
        [InlineData(120, true, TableBorder.Rounded)]
        [InlineData(60, true, TableBorder.Square)]
        [InlineData(120, false, TableBorder.Square)]
        [InlineData(60, false, TableBorder.Double)]
        [InlineData(120, true, TableBorder.Double)]
        public void SectionGroupsSpanColumnsWithoutBorderCollisions(int width, bool light, TableBorder border) {
            Table table = SectionTable();
            table.Border = border;
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(width, 40, ColorDepth.TrueColor, GlyphTier.Full), light ? Theme.Light : Theme.Dark);
            CellBuffer frame = FrameComposer.RenderBlock(table, width, context);
            BoxGlyphs box = context.Glyphs.GetBox(border == TableBorder.Double ? BoxStyle.Double : BoxStyle.Single);
            int column = frame.GetRowText(1).IndexOf(box.Vertical, 1, StringComparison.Ordinal);
            Assert.Equal(box.BottomJoint, frame[column, 2].Glyph);
            Assert.DoesNotContain(box.Cross, frame.GetRowText(2));
            foreach (string label in new[] { "Method", "Event", "Property" }) {
                string group = label + " · 1";
                int y = Assert.Single(Enumerable.Range(0, frame.Height), row => frame.GetRowText(row).Contains(group));
                string line = frame.GetRowText(y);
                Assert.Equal(2, line.Count(character => character.ToString() == box.Vertical));
                int x = line.IndexOf(group, StringComparison.Ordinal);
                Assert.Equal(context.Style(ThemeToken.Accent).With(TextDecoration.Bold), frame[x, y].Style);
                int count = x + group.Length - 1;
                Assert.NotEqual(column, count);
                Assert.Equal(context.Style(ThemeToken.Muted), frame[count, y].Style);
                Assert.Equal(box.Vertical, frame[column, y + 1].Glyph);
            }
        }

        [Fact]
        public void SectionGroupsWithoutHeadersDoNotLeaveTopRuleStubs() {
            Table table = SectionTable();
            table.ShowHeader = false;
            string[] lines = Render(table, 60).Split('\n');
            Assert.DoesNotContain('┬', lines[0]);
            Assert.Contains("Method · 1", lines[1]);
        }

        [Fact]
        public void SectionGroupWindowsAndChunksKeepTheSameCleanBreak() {
            Table table = SectionTable();
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(60, 40, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            CellBuffer group = table.RenderWindow(60, 2, 2, context, out int head, out _, out _);
            Assert.Contains('┴', group.GetRowText(head - 1));
            Assert.DoesNotContain('┼', group.GetRowText(head - 1));
            Assert.Contains("Event · 1", group.GetRowText(head));
            CellBuffer data = table.RenderWindow(60, 3, 1, context, out head, out _, out _);
            Assert.Contains('┼', data.GetRowText(head - 1));
            string chunks = string.Join("\n", table.RenderChunks(60, context, 2).Select(block => block.ToPlainText()));
            Assert.Equal(FrameComposer.RenderBlock(table, 60, context).ToPlainText(), chunks);
        }

        private static Table SectionTable() {
            Table table = new Table { GroupHeadersAsRows = true, RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Name") { Width = 9 });
            table.AddColumn("Definition");
            foreach (string label in new[] { "Method", "Event", "Property" }) {
                table.AddGroup(label);
                table.AddPlainRow("Member", "definition");
            }
            return table;
        }

        [Fact]
        public void Render_TitledSingleRecord_PreservesKeyFieldAndValue() {
            Table table = new Table { Title = "Per-CA" };
            table.AddColumn("CA");
            table.AddColumn("Issued");
            table.AddValues("CA[1]", 42);

            string text = Render(table, 100);

            Assert.Contains("Per-CA", text);
            Assert.Contains("CA[1]", text);
            Assert.Contains("Issued", text);
            Assert.Contains("42", text);
        }

        [Fact]
        public void NumericColumnsAndTheirHeadersAreRightAligned() {
            Table table = new Table { Border = TableBorder.None };
            table.AddColumn("Name");
            table.AddColumn("Count");
            table.AddPlainRow("alpha", "7");
            table.AddPlainRow("beta", "1,234");
            string[] lines = Render(table, 60).Split('\n');
            Assert.Equal("Name   Count", lines[0]);
            Assert.Equal("alpha      7", lines[1]);
            Assert.Equal("beta   1,234", lines[2]);
        }

        [Fact]
        public void ExpandedTableGivesSpareWidthToTheFlexibleColumn() {
            Table table = new Table { Expand = true };
            table.AddColumn(new TableColumn("Id") { Wrap = false });
            table.AddColumn(new TableColumn("Description") { Flexible = true });
            table.AddPlainRow("1", "short");
            string[] lines = Render(table, 50).Split('\n');
            Assert.Equal(50, lines[0].Length);
            Assert.Equal("│ Id │ Description", lines[1].Substring(0, 18));
        }

        [Fact]
        public void LongValuesWrapInsteadOfTruncatingWhileHeadersStayWhole() {
            Table table = new Table();
            table.AddColumn(new TableColumn("Host") { Wrap = false });
            table.AddColumn("Distinguished name");
            table.AddPlainRow("dc01", "CN=DC01,OU=Domain Controllers,DC=ad,DC=evotec,DC=xyz");
            string text = Render(table, 40);
            Assert.Contains("Distinguished name", text);
            Assert.DoesNotContain("…", text);
            Assert.Contains("DC=xyz", text);
            Assert.True(text.Split('\n').All(line => line.Length <= 40));
        }

        [Fact]
        public void StatusCellCarriesTheMarkerAndWord() {
            Table table = new Table { Border = TableBorder.None };
            table.AddColumn("Check");
            table.AddColumn("Result");
            table.AddRow("SPF", TableCell.ForStatus(StatusKind.Fail));
            string ascii = Terminal.RenderText(table, TerminalProfile.Fixed(40, 10, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            Assert.Contains("[FAIL] FAIL", ascii);
        }
    }
}
