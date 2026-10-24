using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>Explicit maxima keep signed bars and their shared zero baseline inside the chart.</summary>
    public sealed class SignedBarChartTests {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-2)]
        public void NonpositiveMaximumIncludesZeroAndKeepsNegativeBarsVisible(double maximum) {
            BarChart chart = new BarChart { Signed = true, Maximum = maximum, ShowValues = false };
            chart.Add("loss", -1).Add("zero", 0).Add("gain", 1);
            string[] rows = Render(chart).Split('\n');
            Assert.EndsWith("|", rows[0], StringComparison.Ordinal);
            Assert.Contains("▆", rows[0].Substring(5, 26), StringComparison.Ordinal);
            Assert.DoesNotContain("▆", rows[1], StringComparison.Ordinal);
            Assert.DoesNotContain("▆", rows[2], StringComparison.Ordinal);
            Assert.All(rows, row => Assert.Equal(31, row.IndexOf('|')));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonfiniteMaximumUsesFiniteSamplesForSignedAndUnsignedBars(double maximum) {
            foreach (bool signed in new[] { false, true }) {
                BarChart chart = new BarChart { Signed = signed, Maximum = maximum, ShowValues = false };
                BarChart automatic = new BarChart { Signed = signed, ShowValues = false };
                foreach (BarChart bars in new[] { chart, automatic }) {
                    bars.Add("loss", -2).Add("zero", 0).Add("gain", 2);
                }
                Assert.Equal(Render(automatic), Render(chart));
                string[] rows = Render(chart).Split('\n');
                Assert.Contains("▆", rows[2], StringComparison.Ordinal);
                if (signed) {
                    Assert.Contains("▆", rows[0], StringComparison.Ordinal);
                    Assert.All(rows, row => Assert.Equal(18, row.IndexOf('|')));
                }
            }
        }

        [Fact]
        public void ExtremeFiniteSignedBoundsKeepBothBarsAndTheBaselineVisible() {
            BarChart chart = new BarChart { Signed = true, Maximum = double.MaxValue, ShowValues = false };
            chart.Add("loss", -double.MaxValue).Add("gain", double.MaxValue).Add("zero", 0);
            string[] rows = Render(chart).Split('\n');
            Assert.All(rows, row => Assert.Equal(18, row.IndexOf('|')));
            Assert.Contains("▆", rows[0].Substring(5, 13), StringComparison.Ordinal);
            Assert.Contains("▆", rows[1].Substring(19), StringComparison.Ordinal);
            Assert.DoesNotContain("▆", rows[2], StringComparison.Ordinal);
        }

        private static string Render(BarChart chart) {
            Canvas.RenderContext context = Terminal.CreateContext(TestTerminal.Profile(32, colors: ColorDepth.Ansi16), Theme.Dark);
            return FrameComposer.RenderBlock(chart, 32, context).ToPlainText();
        }
    }
}
