using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Glyphs;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnframedColumnsKeepSeparationAtTheirSharedEdge(bool grid) {
            TextBlock left = new TextBlock("x") { Alignment = HorizontalAlignment.Right };
            TextBlock right = new TextBlock("y");
            IWidget layout = grid ? new LayoutGrid(2, left, right) { MinColumnWidth = 1 }
                : new Columns(left, right) { CollapseBelow = 0 };
            Assert.Equal("  x y", Render(layout, 7));
            if (layout is LayoutGrid tiles) { tiles.ColumnGap = 0; }
            else { ((Columns)layout).Gap = 0; }
            Assert.Equal("  xy", Render(layout, 6));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AdaptiveTableRecordsKeepSeparationEvenWithABoxedBorderSetting(bool grid) {
            Table left = new Table { Title = "xxxxxxxxxx" }.AddColumn("A").AddColumn("B").AddColumn("C").AddRow("1", "2", "3");
            Table right = new Table { Title = "y" }.AddColumn("A").AddColumn("B").AddColumn("C").AddRow("1", "2", "3");
            IWidget layout = grid ? new LayoutGrid(2, left, right) { MinColumnWidth = 1 }
                : new Columns { CollapseBelow = 0 }.Add(left, LayoutSize.Fixed(10)).Add(right, LayoutSize.Fixed(10));
            string text = Render(layout, 20);
            Assert.DoesNotContain("│", text); // Both tables selected their unframed record view.
            Assert.Contains("xxxxxxxxxx y", text);
        }

        [Theory]
        [InlineData(false, 7, 1)]
        [InlineData(true, 7, 1)]
        [InlineData(false, 3, 3)]
        [InlineData(true, 3, 3)]
        [InlineData(true, 7, 4)]
        public void CrampedPanelsKeepSeparationWhenTheirBordersCannotFit(bool grid, int width, int height) {
            Panel left = new Panel(new TextBlock("x") { Alignment = HorizontalAlignment.Right });
            Panel right = new Panel(new TextBlock("y"));
            LayoutGrid tiles = new LayoutGrid(2, left, right) { MinColumnWidth = 1 };
            if (height == 4) { tiles.Add(left).Add(right); } // The last grid row is clipped to one cell high.
            IWidget layout = grid ? tiles : new Columns(left, right) { CollapseBelow = 0 };
            string text = TestTerminal.Render(layout, width, height, theme: Compact).ToPlainText();
            Assert.Contains("x y", text);
            Assert.DoesNotContain("xy", text);
        }

        [Fact]
        public void FramedTilesCanTouchAndExplicitGapsStayLiteral() {
            Panel a = new Panel(new TextBlock("a"));
            Panel b = new Panel(new TextBlock("b"));
            Columns columns = new Columns().Add(new Sized(a, LayoutSize.Fixed(3))).Add(new Prioritized(b, 1), LayoutSize.Fixed(3));
            Assert.Contains("│a││b│", Render(columns, 6));
            columns.Gap = 1;
            Assert.Contains("│a│ │b│", Render(columns, 7));
            LayoutGrid grid = new LayoutGrid(2, a, b) { MinColumnWidth = 3 };
            Assert.Contains("│a││b│", Render(grid, 6));
            grid.ColumnGap = 1;
            Assert.Contains("│a│ │b│", Render(grid, 7));
            a.Border = BoxStyle.None;
            b.Border = BoxStyle.None;
            Assert.Equal("a b", Render(new LayoutGrid(2, a, b) { MinColumnWidth = 1 }, 3));
        }
    }
}
