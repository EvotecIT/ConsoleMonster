using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class BarChartValueWidthTests {
        [Theory]
        [InlineData(GlyphTier.Full, false, 60)]
        [InlineData(GlyphTier.Ascii, false, 60)]
        [InlineData(GlyphTier.Full, true, 60)]
        [InlineData(GlyphTier.Ascii, true, 60)]
        [InlineData(GlyphTier.Full, false, 12)]
        [InlineData(GlyphTier.Ascii, false, 12)]
        [InlineData(GlyphTier.Full, true, 12)]
        [InlineData(GlyphTier.Ascii, true, 12)]
        public void Render_LongMissingExplanation_PreservesIdentityAndStatusWithinCurrentWidth(GlyphTier tier, bool signed, int width) {
            BarChart chart = Missing(signed);
            string row = TestTerminal.RenderText(chart, width, glyphs: tier).Split('\n')[0];
            Assert.Contains(width == 60 ? "waiting" : "w", row);
            Assert.Contains(GlyphSet.For(tier).StatusSymbol(StatusKind.Warn), row);
            Assert.Contains(tier == GlyphTier.Ascii ? "..." : "…", row);
            Assert.DoesNotContain(tier == GlyphTier.Ascii ? "#" : "▆", row);
            Assert.True(ConsoleMonster.Text.DisplayWidth.Measure(row) <= width);
        }

        [Theory]
        [InlineData(GlyphTier.Full, false)]
        [InlineData(GlyphTier.Ascii, false)]
        [InlineData(GlyphTier.Full, true)]
        [InlineData(GlyphTier.Ascii, true)]
        public void Render_NumericUpdateAfterLongMissingExplanation_DoesNotRetainTheExplanationWidth(GlyphTier tier, bool signed) {
            BarChart chart = Missing(signed);
            TestTerminal.RenderText(chart, 60, glyphs: tier);
            chart.Add("waiting", 7);
            chart.Items[0].Status = StatusKind.Pass;
            BarChart fresh = new BarChart { Signed = signed };
            fresh.Add("waiting", 7);
            fresh.Items[0].Status = StatusKind.Pass;
            if (signed) fresh.Add("reference", -2);
            foreach (int width in new[] { 60, 120 }) {
                string text = TestTerminal.RenderText(chart, width, glyphs: tier);
                Assert.Equal(TestTerminal.RenderText(fresh, width, glyphs: tier), text);
                Assert.Contains("waiting", text);
                Assert.Contains(GlyphSet.For(tier).StatusSymbol(StatusKind.Pass) + " 7", text);
                Assert.Contains(tier == GlyphTier.Ascii ? "#" : "▆", text);
            }
        }

        private static BarChart Missing(bool signed) {
            BarChart chart = new BarChart { Signed = signed };
            chart.AddMissing("waiting", new string('m', 80), StatusKind.Warn);
            if (signed) chart.Add("reference", -2);
            return chart;
        }
    }
}
