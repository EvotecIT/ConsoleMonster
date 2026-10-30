using System;
using System.Collections.Generic;
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
    public sealed class ChartCompositionTests {
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
            Assert.False(Chart.TryNumber(value, out double number));
            Assert.Equal(0, number);
        }

        [Fact]
        public void NumericConversionIsInvariantAndRejectsNonNumbers() {
            Assert.True(Chart.TryNumber("-1.25e2", out double value));
            Assert.Equal(-125, value);
            Assert.True(Chart.TryNumber(12.5m, out value));
            Assert.Equal(12.5, value);
            Assert.False(Chart.TryNumber(DateTime.MinValue, out _));
            Assert.False(Chart.TryNumber(DayOfWeek.Monday, out _));
            Assert.False(Chart.TryNumber(new[] { 1, 2 }, out _));
        }


        [Fact]
        public void RepeatedLabelsRemainDistinctWithoutReplacingEarlierValues() {
            var labels = new HashSet<string>(StringComparer.Ordinal);
            var options = new ChartOptions();
            options.AddItem(Chart.UniqueLabel("same", labels), 2);
            options.AddItem(Chart.UniqueLabel("same", labels), 3);
            options.AddItem(Chart.UniqueLabel("same (2)", labels), 4);
            var chart = Assert.IsType<BarChart>(Chart.Create(options));
            Assert.Equal(new[] { "same", "same (2)", "same (2) (2)" }, chart.Items.Select(item => item.Label));
            Assert.Equal(new[] { 2.0, 3, 4 }, chart.Items.Select(item => item.Value));
        }

        [Fact]
        public void NumericSamplesAndNearbyHistogramEdgesRetainDoublePrecision() {
            double first = BitConverter.Int64BitsToDouble(0x3ff0000000000001);
            double second = BitConverter.Int64BitsToDouble(0x3ff0000000000002);
            Assert.True(Chart.TryNumber(first, out double parsed));
            Assert.Equal(first, parsed);
            var bars = Assert.IsType<BarChart>(Chart.Create(new ChartOptions().AddItem("value", parsed)));
            Assert.Equal(first, Assert.Single(bars.Items).Value);
            var histogram = Assert.IsType<Histogram>(Chart.Create(new ChartOptions {
                Type = ChartType.Histogram, Edges = new[] { first, second }, Values = new[] { first, second }
            }));
            Assert.Equal(2, Assert.Single(histogram.Counts));
        }

        [Fact]
        public void SparklineRetainsMoreThanDefaultCapacityAndEscapesLabels() {
            double[] values = Enumerable.Range(1, 1025).Select(value => (double)value).ToArray();
            var chart = Assert.IsType<Sparkline>(Chart.Create(new ChartOptions {
                Type = ChartType.Sparkline, Title = global::ConsoleMonster.Text.Markup.Escape("[red]host"), Capacity = values.Length, Values = values
            }));
            Assert.Equal(values, chart.Values);
            string wide = TestTerminal.RenderText(chart, 1100, glyphs: GlyphTier.Full);
            Assert.Equal(1025, wide.Count(c => c >= '▁' && c <= '█'));
            Assert.Contains("[red]host", wide, StringComparison.Ordinal);
        }

        [Fact]
        public void HistogramCountsEveryBoundaryIncludingMaximum() {
            var histogram = Assert.IsType<Histogram>(Chart.Create(new ChartOptions {
                Type = ChartType.Histogram, Edges = new[] { -2.0, -1, 0, 1, 2 }, Values = new[] { -2.0, -1, 0, 1, 2 }
            }));
            Assert.Equal(new[] { 1, 1, 1, 2 }, histogram.Counts);
        }

        [Fact]
        public void FractionalHistogramUsesExplicitBinsAndExactCounts() {
            var histogram = Assert.IsType<Histogram>(Chart.Create(new ChartOptions {
                Type = ChartType.Histogram, Edges = new[] { 0.1, 0.3, 0.5 }, Values = new[] { 0.1, 0.3, 0.5 }
            }));
            Assert.Equal(new[] { 1, 2 }, histogram.Counts);
        }

        [Fact]
        public void ExtremeHistogramEdgesStayFiniteAndPreserveSampleCount() {
            var histogram = Assert.IsType<Histogram>(Chart.Create(new ChartOptions {
                Type = ChartType.Histogram,
                Edges = new[] { -double.MaxValue, -double.MaxValue / 2, 0, double.MaxValue / 2, double.MaxValue },
                Values = new[] { -double.MaxValue, 0, double.MaxValue }
            }));
            Assert.Equal(4, histogram.Counts.Count);
            Assert.Equal(3, histogram.Counts.Sum());
            Assert.DoesNotContain("Infinity", TestTerminal.RenderText(histogram), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonfiniteScalarsAreRejectedWithoutFabricatingZero(double value) {
            Assert.False(Chart.TryNumber(value, out double parsed));
            Assert.Equal(0, parsed);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ExistingRendererFramesIncludeSignedBarsHistogramAndTrend(GlyphTier tier) {
            IWidget bars = Chart.Create(new ChartOptions { Signed = true }.AddItem("loss", -2).AddItem("gain", 4).AddItem("zero", 0));
            IWidget histogram = Chart.Create(new ChartOptions {
                Type = ChartType.Histogram, Edges = new[] { 0.0, 1, 2 }, Values = new[] { 0.0, 1, 2 }
            });
            IWidget trend = Chart.Create(new ChartOptions { Type = ChartType.Sparkline, Title = "api", Values = new[] { 1.0, 3, 2 } });
            var scene = new Rows(bars, histogram, new Columns().Add(trend, LayoutSize.Fixed(9)));
            Golden.Match("charts/pipeline." + tier.ToString().ToLowerInvariant() + ".txt", TestTerminal.RenderText(scene, 40, glyphs: tier));
        }

        [Fact]
        public void ColouredChartSharesSignedZeroBaseline() {
            IWidget chart = Chart.Create(new ChartOptions { Signed = true }.AddItem("loss", -4).AddItem("gain", 4).AddItem("zero", 0));
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
