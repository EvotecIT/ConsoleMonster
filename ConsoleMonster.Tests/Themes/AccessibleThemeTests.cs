using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class AccessibleThemeTests {
        /// <summary>Automatic chart traces and full-block stacked segments retain separate foregrounds at each colour tier.</summary>
        [Theory]
        [InlineData(ColorDepth.TrueColor)]
        [InlineData(ColorDepth.Ansi256)]
        [InlineData(ColorDepth.Ansi16)]
        public void ColorBlindSafe_ChartSeriesHaveDistinctForegrounds(ColorDepth depth) {
            ThemeToken[] series = { ThemeToken.Series1, ThemeToken.Series2, ThemeToken.Series3, ThemeToken.Series4, ThemeToken.Series5, ThemeToken.Series6 };
            Color[] colours = series.Select(token => Theme.ColorBlindSafe.Resolve(token, depth).Foreground).ToArray();
            Assert.All(colours, colour => Assert.False(colour.IsDefault));
            Assert.Equal(series.Length, colours.Distinct().Count());
        }

        [Theory]
        [InlineData(ColorDepth.TrueColor)]
        [InlineData(ColorDepth.Ansi256)]
        [InlineData(ColorDepth.Ansi16)]
        [InlineData(ColorDepth.None)]
        public void ColorBlindSafe_PreservesStatusMarkersAndLowColourLegibility(ColorDepth depth) {
            Theme theme = Theme.FromName("ColorBlindSafe");
            Assert.Same(Theme.ColorBlindSafe, theme);
            Style pass = theme.Resolve(ThemeToken.Pass, depth);
            Style fail = theme.Resolve(ThemeToken.Fail, depth);
            if (depth == ColorDepth.None) {
                Assert.Equal(Style.Plain, pass);
                Assert.Equal(Style.Plain, fail);
            } else {
                Assert.NotEqual(pass.Foreground, fail.Foreground);
                Assert.Equal(TextDecoration.Bold, pass.Decoration);
                Assert.True((fail.Decoration & TextDecoration.Underline) != 0);
            }
            foreach (GlyphTier tier in Enum.GetValues(typeof(GlyphTier))) {
                IGlyphSet glyphs = GlyphSet.For(tier);
                RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 10, depth, tier), theme);
                StyledLine passed = StatusLine.Compose(context, RichText.Plain("Completed"), StatusKind.Pass, false, false, false, "HH:mm:ss", DateTime.MinValue, 0, true);
                StyledLine failed = StatusLine.Compose(context, RichText.Plain("Failed"), StatusKind.Fail, false, false, false, "HH:mm:ss", DateTime.MinValue, 0, true);
                Assert.StartsWith(glyphs.StatusMarker(StatusKind.Pass), passed.PlainText);
                Assert.StartsWith(glyphs.StatusMarker(StatusKind.Fail), failed.PlainText);
                Assert.NotEqual(passed[0].Text, failed[0].Text);
                Assert.Equal(pass, passed[0].Style);
                Assert.Equal(fail, failed[0].Style);
            }
        }
    }
}
