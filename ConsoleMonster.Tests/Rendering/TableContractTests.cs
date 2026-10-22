using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>The table sizing rules from the report quality bar, applied to console tables.</summary>
    public sealed class TableContractTests {
        private static string Render(Table table, int width) {
            return Terminal.RenderText(table, TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Full), Theme.Dark);
        }

        /// <summary>Section groups span every column and keep the next data row's column borders intact.</summary>
        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void SectionGroupsSpanColumnsWithoutBorderCollisions(int width) {
            Table table = new Table { GroupHeadersAsRows = true, RecordViewRows = 0 };
            table.AddColumn("Name");
            table.AddColumn("Definition");
            table.AddGroup("Method");
            table.AddPlainRow("Run", "void Run()");
            table.AddGroup("Property");
            table.AddPlainRow("IsBusy", "bool IsBusy {get;}");
            string[] lines = Render(table, width).Split('\n');
            foreach (string group in new[] { "Method  1", "Property  1" }) {
                string line = Assert.Single(lines, value => value.Contains(group));
                Assert.Equal(2, line.Count(character => character == '│'));
                Assert.DoesNotContain('┼', line);
                Assert.DoesNotContain('─', line);
            }
            Assert.Contains(lines, line => line.Contains("void Run()") && line.Count(character => character == '│') == 3);
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
