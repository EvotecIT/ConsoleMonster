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
            Color[] colours = series.Select(token => Theme.ColorBlindSafe.Resolve(token, depth).Color).ToArray();
            Assert.All(colours, colour => Assert.False(colour.IsDefault));
            Assert.Equal(series.Length, colours.Distinct().Count());
        }

        [Theory]
        [InlineData("ColorBlindSafe", ColorDepth.TrueColor)]
        [InlineData("ColorBlindSafe", ColorDepth.Ansi256)]
        [InlineData("ColorBlindSafe", ColorDepth.Ansi16)]
        [InlineData("ColorBlindSafe", ColorDepth.None)]
        [InlineData("ColorBlindSafeLight", ColorDepth.TrueColor)]
        [InlineData("ColorBlindSafeLight", ColorDepth.Ansi256)]
        [InlineData("ColorBlindSafeLight", ColorDepth.Ansi16)]
        public void ColorBlindSafe_PreservesStatusMarkersAndLowColourLegibility(string name, ColorDepth depth) {
            Theme theme = Theme.FromName(name);
            Assert.Equal(name, theme.Name);
            Style pass = theme.Resolve(ThemeToken.Pass, depth);
            Style fail = theme.Resolve(ThemeToken.Fail, depth);
            if (depth == ColorDepth.None) {
                Assert.Equal(Style.Plain, pass);
                Assert.Equal(Style.Plain, fail);
            } else {
                Assert.NotEqual(pass.Color, fail.Color);
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

        /// <summary>Presets report their real status separation; the accessible variants also meet every contrast and fill rule.</summary>
        [Theory]
        [InlineData("Dark", false)]
        [InlineData("Light", false)]
        [InlineData("HighContrast", false)]
        [InlineData("ColorBlindSafe", true)]
        [InlineData("ColorBlindSafeLight", true)]
        [InlineData("ConsoleViewDark", false)]
        [InlineData("ConsoleViewLight", false)]
        public void PresetsReportTheirActualAccessibility(string name, bool colourBlindSafe) {
            ITheme theme = name == "ConsoleViewDark" ? Theme.ConsoleViewDark : name == "ConsoleViewLight" ? Theme.ConsoleViewLight : Theme.FromName(name);
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.Equal(colourBlindSafe, report.Passed);
            Assert.Equal(colourBlindSafe, report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision).All(result => result.Outcome == ThemeCheckOutcome.Pass));
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
            if (colourBlindSafe) {
                Assert.All(report.Results, result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
                double minimum = report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision && result.Detail.Contains("tritanopia")).Min(result => result.Value!.Value);
                Assert.InRange(minimum, name == "ColorBlindSafe" ? 59.7 : 46.5, name == "ColorBlindSafe" ? 59.8 : 46.6);
            }
        }

        [Fact]
        public void ColorBlindPreference_ResolvesItsLightAndDarkVariants() {
            TerminalProfile profile = TerminalProfile.Fixed(80, 10, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Same(Theme.ColorBlindSafeLight, Terminal.ResolveTheme(profile.WithThemePreference(ThemePreference.ColorBlindLight)));
            Assert.Same(Theme.ColorBlindSafe, Terminal.ResolveTheme(profile.WithThemePreference(ThemePreference.ColorBlindDark)));
            // Non-interactive output has no detected background, so the automatic variant is the dark one.
            Assert.Same(Theme.ColorBlindSafe, Terminal.ResolveTheme(profile.WithThemePreference(ThemePreference.ColorBlind)));
        }

    }
}
