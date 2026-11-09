using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Frames;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    public sealed partial class ConsistencyTests {
        public static IEnumerable<object[]> DesignProfiles() {
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii })
            foreach (string theme in new[] { "Dark", "Light", "ColorBlindSafe" })
            foreach (ThemeDensity density in new[] { ThemeDensity.Comfortable, ThemeDensity.Compact })
            foreach (StatusMarkerStyle markers in new[] { StatusMarkerStyle.Symbols, StatusMarkerStyle.Words })
                yield return new object[] { tier, theme, density, markers };
        }

        [Theory]
        [MemberData(nameof(DesignProfiles))]
        public void BarsAndHierarchyShareTheirMeaningAcrossProfiles(GlyphTier tier, string themeName, ThemeDensity density, StatusMarkerStyle markers) {
            ITheme theme = themeName == "Dark" ? Theme.Dark : themeName == "Light" ? Theme.Light : Theme.ColorBlindSafe;
            var profile = TerminalProfile.Fixed(90, 24, ColorDepth.TrueColor, tier).WithDensity(density).WithStatusMarkers(markers);
            RenderContext context = Terminal.CreateContext(profile, theme);
            CellBuffer Render(IWidget widget, int height = 1) {
                CellBuffer cells = new CellBuffer(90, height);
                widget.Render(new Region(cells), context);
                return cells;
            }
            Assert.False(context.Style(ThemeToken.Critical).BackgroundColor.IsDefault);
            var quota = new BarChart { Maximum = 100 };
            quota.Add("quota", 72);
            CellBuffer line = Render(quota);
            Assert.Contains("72%", line.GetRowText(0));
            Assert.DoesNotContain("72/100", line.GetRowText(0));
            Assert.Equal(context.Style(ThemeToken.Muted), line[0, 0].Style);
            string track = tier == GlyphTier.Ascii ? "-" : "─";
            Assert.Contains(Enumerable.Range(0, 90), x => line[x, 0].Glyph == track && line[x, 0].Style.Equals(context.Style(ThemeToken.Subtle)));
            quota.Maximum = 200;
            quota.Unit = "ms";
            Assert.Contains("72/200ms", Render(quota).GetRowText(0));
            quota.Maximum = null;
            CellBuffer comparison = Render(quota);
            Assert.DoesNotContain(track, comparison.GetRowText(0));
            Assert.Contains(tier == GlyphTier.Full ? "▆" : tier == GlyphTier.Basic ? "▄" : "#", comparison.GetRowText(0));
            Assert.DoesNotContain("72/100", Render(new ProgressBar { Value = 72 }).GetRowText(0));
            var composition = new StackedBar { ShowLegend = false };
            composition.Add("Passed", 8).Add("Failed", 2);
            CellBuffer segments = Render(composition);
            Assert.Equal(context.Style(ThemeToken.Muted), segments[0, 0].Style);
            Assert.Contains(Enumerable.Range(0, 90), x => segments[x, 0].Style.Equals(context.Style(ThemeToken.Fail)));
            Assert.EndsWith("10", segments.GetRowText(0).TrimEnd());


            var facts = Ui.Facts().Add(3, "passed", ThemeToken.Pass).Add(1, "warning", ThemeToken.Warn).Add(5, "checks");
            CellBuffer counts = Render(facts);
            string text = counts.GetRowText(0);
            int warning = text.IndexOf("1 warning", StringComparison.Ordinal);
            Assert.True(warning >= 0);
            Assert.Equal(context.Style(ThemeToken.Warn).With(TextDecoration.Bold), counts[warning, 0].Style);
            Assert.Equal(context.Style(ThemeToken.Warn), counts[warning + 2, 0].Style);
            Assert.Contains(tier == GlyphTier.Ascii ? " | " : " · ", text);
            Assert.Equal(tier == GlyphTier.Ascii ? "!" : "▲", context.Glyphs.StatusSymbol(StatusKind.Warn));
            Assert.Equal(1, DisplayWidth.Measure(context.Glyphs.StatusSymbol(StatusKind.Warn), TerminalWidthProfile.Standard));

            var meter = Meter.Count("Servers", 48);
            StyledLine[] lines = meter.Lines(30, context);
            Assert.Equal(context.Style(ThemeToken.Muted), lines[0][0].Style);
            Assert.Equal(context.Style(ThemeToken.Strong), lines[1][0].Style);
            CellBuffer keys = Render(new KeyValueList().Add("Servers", "48"));
            Assert.Equal(context.Style(ThemeToken.Muted), keys[0, 0].Style);
            Assert.Equal(context.Style(ThemeToken.Text), keys[9, 0].Style);
        }

        [Fact]
        public void FactsKeepPresenterLinksAndDecorationsWhenApplyingTheirSemanticTone() {
            var facts = Ui.Facts().Add(new ViewFact("https://example.test/report", "report",
                presenter: ConsoleMonster.Presenters.CellPresenters.Link, role: ThemeToken.Warn));
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(80, 1).WithHyperlinks(true), Theme.Dark);
            CellBuffer cells = new CellBuffer(80, 1);
            facts.Render(new Region(cells), context);
            var linked = Enumerable.Range(0, 80).Select(x => cells[x, 0]).Where(c => c.Style.Link != null).ToArray();
            Assert.NotEmpty(linked);
            Assert.All(linked, cell => {
                Assert.Equal("https://example.test/report", cell.Style.Link);
                Assert.Equal(context.Style(ThemeToken.Warn).Color, cell.Style.Color);
                Assert.True((cell.Style.Decoration & (TextDecoration.Underline | TextDecoration.Bold)) == (TextDecoration.Underline | TextDecoration.Bold));
            });
        }

        [Fact]
        public void DashboardFillsItsLastSectionWithoutPaddingPrintedReportsOrOrdinaryRows() {
            var body = new Rows(new TextBlock("Summary"), new Panel(new TextBlock("Details"), "Section"));
            var dashboard = new Dashboard("Live", body);
            RenderContext context = Context(GlyphTier.Full);
            int natural = dashboard.Measure(new Constraints(90), context).Height;
            Assert.True(natural < 24);
            CellBuffer full = new CellBuffer(90, 24);
            dashboard.Render(new Region(full), context);
            Assert.Contains("╰", full.GetRowText(22));
            Assert.Contains("ctrl+c", full.GetRowText(23));
            Assert.Equal(natural, dashboard.Measure(new Constraints(90), context).Height);
            CellBuffer ordinary = new CellBuffer(90, 24);
            body.Render(new Region(ordinary), context);
            Assert.Contains("╰", ordinary.GetRowText(body.Measure(new Constraints(90), context).Height - 1));
            Assert.DoesNotContain("╰", ordinary.GetRowText(23));
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LabelledChartsShowSparseAxisAndPeakWithoutRepeatedPanelNames(bool histogram) {
            var spark = new Sparkline { Title = "errors", Height = 3 };
            for (int hour = 0; hour < 24; hour++) {
                spark.Add(hour == 14 ? 44 : hour);
                spark.Labels.Add(hour.ToString("00") + ":00");
            }
            var distribution = new Histogram(Enumerable.Range(0, 24).Select(hour => (double)hour).ToArray()) { Height = 3 };
            foreach (string label in spark.Labels) distribution.Labels.Add(label);
            for (int i = 0; i < 44; i++) distribution.Add(14);
            var panel = new Panel(histogram ? (IWidget)distribution : spark, "Errors per hour");
            string text = ConsoleMonster.Testing.TestTerminal.RenderText(panel, 80);
            Assert.Contains("Errors per hour", text);
            Assert.DoesNotContain("errors", text);
            Assert.Contains("00:00", text);
            Assert.Contains("12:00", text);
            Assert.Contains("23:00", text);
            Assert.Contains(histogram ? "44" : "peak 44", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NumericTextTableColumnsAlignRightUnlessExplicitlyAligned(bool raw) {
            Table table = new Table().AddColumn("Count");
            if (raw) { table.AddValues("3"); table.AddValues("18"); }
            else { table.AddRow("3"); table.AddRow("18"); }
            RenderContext context = Context(GlyphTier.Full);
            Assert.Equal(HorizontalAlignment.Right, table.SemanticLayout(context).Alignments[0]);
            table.Columns[0].Alignment = HorizontalAlignment.Left;
            Assert.Equal(HorizontalAlignment.Left, table.SemanticLayout(context).Alignments[0]);
            table.Columns[0].Alignment = null;
            table.AddValues("unknown");
            Assert.Equal(HorizontalAlignment.Left, table.SemanticLayout(context).Alignments[0]);
        }

    }
}
