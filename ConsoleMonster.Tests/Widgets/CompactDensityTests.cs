using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CompactDensityTests {
        private static readonly Theme Compact = Theme.Dark.WithAppearance(new ThemeAppearance(ThemeDensity.Compact));

        private static string Render(IWidget widget, int width = 40, GlyphTier tier = GlyphTier.Full, Theme? theme = null) =>
            TestTerminal.Render(widget, width, glyphs: tier, theme: theme ?? Compact).ToPlainText();

        private static Table Checks(TableBorder border) => new Table { Border = border, Expand = false, CardsBelow = 0 }
            .AddColumn("Check").AddColumn("State").AddRow("DNS", "Ready").AddRow("Backup", "Warn");

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void TablesPanelsAndListsUseCompactDefaults(GlyphTier tier) {
            Rows scene = new Rows(
                Checks(TableBorder.Rounded), Checks(TableBorder.Rule),
                new Panel(new TextBlock("No padding"), "Default") { Expand = false },
                new Panel(new TextBlock("Authored padding"), "Explicit") { Expand = false, PaddingLeft = 1, PaddingRight = 2 },
                new StatusList().Add("Build", StatusKind.Pass, "ready").Add("Deploy", StatusKind.Warn, "held"),
                new KeyValueList().Add("Host", "web01").Add("State", "ready"));
            Golden.Match("density/compact." + tier.ToString().ToLowerInvariant() + ".txt", Render(scene, tier: tier));
        }

        [Theory]
        [InlineData(TableBorder.Rounded)]
        [InlineData(TableBorder.Square)]
        [InlineData(TableBorder.Double)]
        public void BoxedTableMeasurementAndDrawingShareTheSamePadding(TableBorder border) {
            Table table = Checks(border);
            RenderContext context = Terminal.CreateContext(TestTerminal.Profile(40), Compact);
            Size size = table.Measure(new Constraints(40), context);
            Assert.Equal(14, size.Width); // 6 + 5 text cells and three borders.
            string[] lines = FrameComposer.RenderBlock(table, size.Width, context).ToPlainText().Split('\n');
            Assert.Contains("DNS   ", lines[3]);
            Assert.All(lines, line => Assert.Equal(size.Width, line.Length));
        }

        [Fact]
        public void UnboxedTablesKeepExplicitColumnGaps() {
            foreach (TableBorder border in new[] { TableBorder.Rule, TableBorder.Minimal, TableBorder.None }) {
                Table table = new Table { Border = border, ShowHeader = false, Expand = false, CardsBelow = 0 }
                    .AddColumn("A").AddColumn(new TableColumn("B") { Gap = 2 }).AddRow("x", "y");
                Assert.Equal("x  y", Render(table));
                table = new Table { Border = border, ShowHeader = false, Expand = false, CardsBelow = 0 }
                    .AddColumn("A").AddColumn("B").AddRow("x", "y");
                Assert.Equal("x y", Render(table));
            }
        }

        [Fact]
        public void PanelSidesAndKeyValueGapsKeepExplicitValuesEqualToTheOldDefault() {
            Panel panel = new Panel(new TextBlock("x")) { Expand = false, PaddingLeft = 1 };
            Assert.Contains("│ x│", Render(panel, 4));
            panel.PaddingRight = 1;
            Assert.Contains("│ x │", Render(panel, 5));
            Assert.Equal("Key value", Render(new KeyValueList().Add("Key", "value")));
            Assert.Equal("Key  value", Render(new KeyValueList { Gap = 2 }.Add("Key", "value")));
            Assert.Equal("Key  value", Render(new KeyValueList().Add("Key", "value"), theme: Theme.Dark));
        }

        [Fact]
        public void StatusDetailsKeepOneSpaceAndNarrowPanelsKeepTheirBorders() {
            Assert.Equal("✓ Build ready", Render(new StatusList().Add("Build", StatusKind.Pass, "ready"), 13));
            Assert.Equal("╭─╮\n│x│\n╰─╯", Render(new Panel(new TextBlock("x")) { Expand = false }, 3));
        }

        [Fact]
        public void TileLayoutsUseZeroDefaultGapAndKeepExplicitGaps() {
            Columns columns = new Columns().Add(new TextBlock("a"), LayoutSize.Fixed(1)).Add(new TextBlock("b"), LayoutSize.Fixed(1));
            Assert.Equal("ab", Render(columns));
            columns.Gap = 1;
            Assert.Equal("a b", Render(columns));
            LayoutGrid grid = new LayoutGrid(2, new TextBlock("a"), new TextBlock("b")) { MinColumnWidth = 1 };
            Assert.Equal("ab", Render(grid, 2));
            grid.ColumnGap = 1;
            Assert.Equal("a b", Render(grid, 3));
        }
    }
}
