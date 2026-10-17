using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>NaN and infinite values (a rate computed over zero elapsed time) never throw and never print "Infinity" or "NaN".</summary>
    public sealed class NonFiniteValueTests {
        private static string Render(IWidget widget, GlyphTier tier = GlyphTier.Full) {
            return Terminal.RenderText(widget, TerminalProfile.Fixed(60, 20, ColorDepth.TrueColor, tier), Theme.Dark);
        }

        private static void AssertReadable(string text) {
            Assert.DoesNotContain("Infinity", text);
            Assert.DoesNotContain("NaN", text);
            Assert.DoesNotContain("∞", text);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Sparkline_NonFiniteValues_AreSkipped(int height) {
            Sparkline sparkline = new Sparkline { Height = height, Unit = "/s" };
            sparkline.AddRange(new[] { 1, 2, double.PositiveInfinity, double.NaN, double.NegativeInfinity, 3 });
            AssertReadable(Render(sparkline));
            Assert.Contains("3/s", Render(sparkline));
        }

        [Fact]
        public void Sparkline_NonFiniteMaximumOrNegativeCapacity_DoesNotThrow() {
            Sparkline sparkline = new Sparkline { Max = double.NaN, Capacity = -5 };
            sparkline.AddRange(new[] { 1.0, 2.0, 3.0 });
            AssertReadable(Render(sparkline));
        }

        [Fact]
        public void StackedBar_InfiniteAndNaNSegments_ShowADash() {
            StackedBar bar = new StackedBar();
            bar.Add("passed", 40);
            bar.Add("rate", double.PositiveInfinity);
            bar.Add("unknown", double.NaN);
            string text = Render(bar);
            AssertReadable(text);
            Assert.Contains("—", text);
            Assert.Contains("100%", text);
            string ascii = Render(bar, GlyphTier.Ascii);
            AssertReadable(ascii);
            Assert.DoesNotContain("—", ascii);
        }

        [Fact]
        public void BarChart_InfiniteValue_ShowsADashAndScalesTheRest() {
            BarChart chart = new BarChart();
            chart.Add("dc01", 10);
            chart.Add("dc02", double.PositiveInfinity);
            string text = Render(chart);
            AssertReadable(text);
            Assert.Contains("—", text);
            string ascii = Render(chart, GlyphTier.Ascii);
            AssertReadable(ascii);
            Assert.DoesNotContain("—", ascii);
        }

        [Fact]
        public void LineChart_NonFiniteValuesAndBounds_DoNotThrow() {
            LineChart chart = new LineChart { Height = 6, Max = double.NaN, Min = double.NegativeInfinity, Unit = "%" };
            chart.Add("cpu", 10);
            chart.Add("cpu", double.PositiveInfinity);
            chart.Add("cpu", double.NaN);
            chart.Add("cpu", 30);
            AssertReadable(Render(chart));

            LineChart extreme = new LineChart { Height = 6 };
            extreme.Add("x", -1e308);
            extreme.Add("x", 1e308);
            AssertReadable(Render(extreme));
        }

        [Fact]
        public void Histogram_NonFiniteValues_AreSkipped() {
            Histogram histogram = new Histogram(new[] { 0.0, 1, 2, 4 });
            histogram.Add(0.5);
            histogram.Add(double.NaN);
            histogram.Add(double.PositiveInfinity);
            AssertReadable(Render(histogram));
        }

        [Fact]
        public void NumberFormat_NonFinite_NeverPrintsInfinity() {
            Assert.Equal("-", NumberFormat.Compact(double.PositiveInfinity));
            Assert.Equal("-", NumberFormat.Grouped(double.NaN));
            Assert.Equal("-", NumberFormat.Percent(double.NaN));
            Assert.Equal("100%", NumberFormat.Percent(double.PositiveInfinity));
        }
    }
}
