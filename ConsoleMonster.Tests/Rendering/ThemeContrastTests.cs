using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>The light theme stays readable on a white background (WCAG contrast: 4.5:1 for text, 3:1 for graphics).</summary>
    public sealed class ThemeContrastTests {
        private static double Contrast(Color color) {
            double foreground = TerminalBackground.CalculateLuminance(color.R, color.G, color.B);
            double white = 1.0;
            return (white + 0.05) / (foreground + 0.05);
        }

        [Theory]
        [InlineData(ThemeToken.Muted)]
        [InlineData(ThemeToken.Accent)]
        [InlineData(ThemeToken.Pass)]
        [InlineData(ThemeToken.Fail)]
        [InlineData(ThemeToken.Warn)]
        [InlineData(ThemeToken.Info)]
        [InlineData(ThemeToken.Skip)]
        [InlineData(ThemeToken.Running)]
        [InlineData(ThemeToken.Title)]
        [InlineData(ThemeToken.Header)]
        public void LightTheme_TextTokens_MeetTextContrastOnWhite(ThemeToken token) {
            Color color = Theme.Light.Resolve(token, ColorDepth.TrueColor).Color;
            Assert.True(Contrast(color) >= 4.5, token + " contrast " + Contrast(color).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(ThemeToken.Border)]
        [InlineData(ThemeToken.Pending)]
        [InlineData(ThemeToken.Series1)]
        [InlineData(ThemeToken.Series2)]
        [InlineData(ThemeToken.Series3)]
        [InlineData(ThemeToken.Series4)]
        [InlineData(ThemeToken.Series5)]
        [InlineData(ThemeToken.Series6)]
        public void LightTheme_GraphicTokens_MeetGraphicContrastOnWhite(ThemeToken token) {
            Color color = Theme.Light.Resolve(token, ColorDepth.TrueColor).Color;
            Assert.True(Contrast(color) >= 3.0, token + " contrast " + Contrast(color).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
