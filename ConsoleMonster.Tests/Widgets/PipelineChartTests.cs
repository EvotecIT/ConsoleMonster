using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class PipelineChartTests {
        [Fact]
        public void BarsSortStablyBeforeTopAndKeepRepeatedLabels() {
            PipelineChart chart = new PipelineChart().WithSort(PipelineChartSort.Descending).WithTop(3)
                .Add("same", 2).Add("first", 8).Add("second", 8).Add("same", 3);
            BarChart bars = Assert.IsType<BarChart>(chart.Build());
            Assert.Equal(new[] { "first", "second", "same" }, bars.Items.Select(i => i.Label));
            Assert.Equal(new[] { 8.0, 8, 3 }, bars.Items.Select(i => i.Value));
            Assert.Equal(new[] { ThemeToken.Series1, ThemeToken.Series2, ThemeToken.Series3 }, bars.Items.Select(i => i.Token!.Value));
            chart.WithTop(0).WithSort(PipelineChartSort.None);
            Assert.Equal(new[] { "same", "first", "second", "same (2)" }, Assert.IsType<BarChart>(chart.Build()).Items.Select(i => i.Label));
            chart.WithSort(PipelineChartSort.Ascending).WithTop(1);
            Assert.Equal(2, Assert.Single(Assert.IsType<BarChart>(chart.Build()).Items).Value);
        }

        [Fact]
        public void SparklineGroupsRetainFirstAppearanceAndSampleOrder() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Sparkline)
                .Add("b", 9).Add("a", 7).Add("b", 2).Add("c", 2).Add("a", 1);
            Rows rows = Assert.IsType<Rows>(chart.Build());
            Assert.Equal(new[] { "b", "a", "c" }, rows.Children.Cast<Columns>().Select(s => Assert.IsType<TextBlock>(s.Children[0]).Content));
            Assert.Equal(new[] { 9.0, 2 }, Trend(rows.Children[0]).Values);
            chart.WithSort(PipelineChartSort.Descending).WithTop(2);
            rows = Assert.IsType<Rows>(chart.Build());
            Assert.Equal(new[] { "b", "c" }, rows.Children.Cast<Columns>().Select(s => Assert.IsType<TextBlock>(s.Children[0]).Content));
            Assert.Equal(new[] { 9.0, 2 }, Trend(rows.Children[0]).Values);
        }

        [Fact]
        public void SparklineRetainsMoreThanDefaultCapacityAndEscapesLabels() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Sparkline);
            for (int i = 0; i < 1025; i++) { chart.Add("[red]host", i); }
            Sparkline trend = Trend(Assert.Single(Assert.IsType<Rows>(chart.Build()).Children));
            Assert.Equal(1025, trend.Values.Count);
            Assert.Equal(0, trend.Values[0]);
            Assert.Contains("[red]host", TestTerminal.RenderText(chart.Build()), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(16)]
        [InlineData(60)]
        [InlineData(120)]
        public void SparklineRowsStartAfterSharedLabelsAndKeepLatestBesideTheirOwnSamples(int width) {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Sparkline).WithUnit("ms")
                .Add("api", 1).Add("longer", 2).Add("api", 3).Add("longer", 1).Add("api", 2);
            string[] lines = TestTerminal.RenderText(chart.Build(), width, glyphs: GlyphTier.Full).Split('\n');
            Assert.Equal(new[] { "api    ▃█▅ 2ms", "longer █▄ 1ms" }, lines);
        }

        [Fact]
        public void SparklineLabelColumnUsesDisplayCellsAndKeepsMarkupLiteral() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Sparkline)
                .Add("日本", 1).Add("[x]", 1);
            string[] lines = TestTerminal.RenderText(chart.Build(), 20, glyphs: GlyphTier.Full).Split('\n');
            Assert.Equal(new[] { "日本 █ 1", "[x]  █ 1" }, lines);
        }

        private static Sparkline Trend(IWidget row) => Assert.IsType<Sparkline>(Assert.IsType<Columns>(row).Children[1]);

        [Fact]
        public void SignedTrendsUseExactTextRatherThanFlatteningNegatives() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Sparkline).Add("delta", -2).Add("delta", 3);
            TextBlock text = Assert.IsType<TextBlock>(Assert.Single(Assert.IsType<Rows>(chart.Build()).Children));
            Assert.Equal("delta: -2, 3", text.Content);
        }

        [Fact]
        public void HistogramHasWholeEdgesAndCountsEveryBoundaryIncludingMaximum() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(4).WithTop(1).WithSort(PipelineChartSort.Descending);
            foreach (double value in new[] { -2.0, -1, 0, 1, 2 }) { chart.Add(null, value); }
            Histogram histogram = Assert.IsType<Histogram>(chart.Build());
            Assert.Equal(new[] { 1, 1, 1, 2 }, histogram.Counts);
            Assert.Equal("-2", histogram.EdgeFormat!(-2));
            Assert.Equal("[1, 2]: 2", Assert.IsType<TextBlock>(Assert.IsType<Rows>(chart.Build(true)).Children[3]).Content);
        }

        [Fact]
        public void SmallIntegerHistogramDoesNotInventFractionalTicks() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(10).Add(null, 1).Add(null, 2);
            Assert.Equal(new[] { 2 }, Assert.IsType<Histogram>(chart.Build()).Counts);
            Assert.Equal("[1, 2]: 2", TestTerminal.RenderText(chart));
        }

        [Fact]
        public void FractionalHistogramUsesRequestedBinsAndExactCounts() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(2)
                .Add(null, 0.1).Add(null, 0.3).Add(null, 0.5);
            Assert.Equal(new[] { 1, 2 }, Assert.IsType<Histogram>(chart.Build()).Counts);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(5.0)]
        [InlineData(-5.0)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public void ConstantHistogramHasOneFiniteNonemptyBucket(double value) {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Histogram).Add(null, value).Add(null, value);
            Assert.Equal(new[] { 2 }, Assert.IsType<Histogram>(chart.Build()).Counts);
            string text = TestTerminal.RenderText(chart);
            Assert.DoesNotContain("Infinity", text, StringComparison.Ordinal);
            Assert.EndsWith("]: 2", text, StringComparison.Ordinal);
            if (value == 5) { Assert.Equal("[2, 5]: 2", text); }
            if (value == -5) { Assert.Equal("[-5, -2]: 2", text); }
        }

        [Fact]
        public void ExtremeHistogramEdgesStayFiniteAndPreserveSampleCount() {
            PipelineChart chart = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(4)
                .Add(null, -double.MaxValue).Add(null, 0).Add(null, double.MaxValue);
            Histogram histogram = Assert.IsType<Histogram>(chart.Build());
            Assert.Equal(4, histogram.Counts.Count);
            Assert.Equal(3, histogram.Counts.Sum());
            Assert.DoesNotContain("Infinity", TestTerminal.RenderText(chart), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(PipelineChartType.Bar)]
        [InlineData(PipelineChartType.Histogram)]
        [InlineData(PipelineChartType.Sparkline)]
        public void EmptyChartHasNoRows(PipelineChartType type) {
            PipelineChart chart = new PipelineChart().WithType(type);
            Assert.Equal(string.Empty, TestTerminal.RenderText(chart));
            Assert.Empty(Assert.IsType<Rows>(chart.Build()).Children);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("bad")]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("1e999")]
        [InlineData(true)]
        [InlineData('2')]
        public void MalformedScalarsAreNotFabricatedAsZero(object? value) {
            Assert.False(PipelineChart.TryNumber(value, out double number));
            Assert.Equal(0, number);
        }

        [Fact]
        public void NumericConversionIsInvariantAndRejectsNonNumbers() {
            Assert.True(PipelineChart.TryNumber("-1.25e2", out double value));
            Assert.Equal(-125, value);
            Assert.True(PipelineChart.TryNumber(12.5m, out value));
            Assert.Equal(12.5, value);
            Assert.False(PipelineChart.TryNumber(DateTime.MinValue, out _));
            Assert.False(PipelineChart.TryNumber(DayOfWeek.Monday, out _));
            Assert.False(PipelineChart.TryNumber(new[] { 1, 2 }, out _));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonfiniteSamplesAreRejectedByCore(double value) {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().Add("bad", value));
            Assert.False(PipelineChart.TryNumber(value, out _));
        }

        [Fact]
        public void InvalidOptionsAreRejectedBeforeRendering() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().WithBins(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().WithBins(129));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().WithTop(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().WithType((PipelineChartType)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PipelineChart().WithSort((PipelineChartSort)99));
        }

        [Fact]
        public void NoColourFramesShowValuesCountsAndEveryTrendSample() {
            PipelineChart bars = new PipelineChart().Add("[host]", -2).Add("api", 3);
            PipelineChart histogram = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(2).Add(null, 0).Add(null, 1).Add(null, 2);
            PipelineChart trends = new PipelineChart().WithType(PipelineChartType.Sparkline).Add("b", 3).Add("a", 2).Add("b", 1);
            string frame = TestTerminal.RenderText(new Rows(bars, histogram, trends), 40);
            Golden.Match("charts/pipeline.text.txt", frame);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ExistingRendererFramesIncludeSignedBarsHistogramAndGroupedTrends(GlyphTier tier) {
            PipelineChart bars = new PipelineChart().Add("loss", -2).Add("gain", 4).Add("zero", 0);
            PipelineChart histogram = new PipelineChart().WithType(PipelineChartType.Histogram).WithBins(2).Add(null, 0).Add(null, 1).Add(null, 2);
            PipelineChart trends = new PipelineChart().WithType(PipelineChartType.Sparkline).Add("api", 1).Add("api", 3).Add("api", 2);
            Rows scene = new Rows(bars.Build(), histogram.Build(), trends.Build());
            Golden.Match("charts/pipeline." + tier.ToString().ToLowerInvariant() + ".txt", TestTerminal.RenderText(scene, 40, glyphs: tier));
        }

        [Fact]
        public void ColouredWidgetSelectsChartRenderersAndSharesSignedZeroBaseline() {
            PipelineChart chart = new PipelineChart().Add("loss", -4).Add("gain", 4).Add("zero", 0);
            Canvas.RenderContext context = Terminal.CreateContext(TestTerminal.Profile(40, colors: ColorDepth.Ansi16), Theme.Dark);
            Canvas.CellBuffer buffer = FrameComposer.RenderBlock(chart, 40, context);
            int zero = buffer.GetRowText(0).IndexOf('|');
            Assert.True(zero > 6);
            Assert.Equal(zero, buffer.GetRowText(1).IndexOf('|'));
            Assert.Equal(zero, buffer.GetRowText(2).IndexOf('|'));
            Assert.Contains("▆", buffer.GetRowText(0).Substring(5, zero - 5), StringComparison.Ordinal);
            Assert.Contains("▆", buffer.GetRowText(1).Substring(zero + 1), StringComparison.Ordinal);
            Assert.DoesNotContain("▆", buffer.GetRowText(2), StringComparison.Ordinal);
            Assert.EndsWith("-4", buffer.GetRowText(0), StringComparison.Ordinal);
        }
    }
}
