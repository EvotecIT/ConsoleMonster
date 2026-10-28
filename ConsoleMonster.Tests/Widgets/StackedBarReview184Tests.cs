using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class StackedBarReview184Tests {
        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.None, 120)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 60)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 120)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60)]
        public void Render_EightCategoriesWithoutColorOrInAscii_UseDistinctFillsMatchingTheirLegends(GlyphTier tier, ColorDepth depth, int width) {
            StackedBar bar = new StackedBar { MaxLegendColumns = 1 };
            for (int i = 0; i < 8; i++) bar.Add("category-" + i, 1);
            Review184Capture capture = new Review184Capture(bar, width, tier, depth);
            HashSet<string> fills = new HashSet<string>();
            string[] lines = capture.Raw.ToPlainText().Split('\n');
            int position = 0;
            for (int i = 0; i < 8; i++) {
                string marker = lines[i + 1].Substring(0, 1);
                Assert.Equal(marker, bar.GetFillMarker(i, capture.Context));
                Assert.Equal(1, DisplayWidth.Measure(marker));
                Assert.True(fills.Add(marker), "Repeated category marker: " + marker);
                Assert.Equal(marker, capture.Raw[position, 0].Glyph);
                Assert.Contains(marker + " category-" + i, capture.Decoded);
                // Equal weights allocate the remainder to the earlier segments.
                position += width / 8 + (i < width % 8 ? 1 : 0);
            }
        }

        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.None)]
        [InlineData(GlyphTier.Basic, ColorDepth.None)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None)]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor)]
        public void GetFillMarker_HiddenBuiltInLegend_ReturnsExactRenderedFillInSegmentOrder(GlyphTier tier, ColorDepth depth) {
            StackedBar bar = new StackedBar { ShowLegend = false };
            bar.Add("empty", 0);
            for (int i = 0; i < 8; i++) bar.Add("state-" + i, 1);
            Review184Capture capture = new Review184Capture(bar, 120, tier, depth);
            Assert.Equal(1, capture.Raw.Height);
            for (int i = 0; i < 8; i++) {
                string marker = bar.GetFillMarker(i + 1, capture.Context);
                Assert.Equal(1, DisplayWidth.Measure(marker));
                Assert.Equal(marker.Trim(), marker);
                Assert.Equal(marker, capture.Raw[i * 15, 0].Glyph);
                Assert.Contains(marker, capture.Decoded);
            }
        }
    }
}
