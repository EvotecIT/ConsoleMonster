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

        /// <summary>
        /// Pass, warning and fail must stay apart for red-green colour blindness, not just in normal vision: each pair keeps a
        /// CIELAB distance of at least 25 after a full-severity protanopia and deuteranopia simulation (Machado et al. 2009),
        /// and each status keeps WCAG 4.5:1 contrast against its intended background.
        /// </summary>
        [Theory]
        [InlineData("ColorBlindSafe", 0x1e, 0x1e, 0x1e)]
        [InlineData("ColorBlindSafeLight", 0xff, 0xff, 0xff)]
        public void ColorBlindSafe_StatusesStayDistinctUnderSimulatedRedGreenColourBlindness(string name, byte r, byte g, byte b) {
            Theme theme = Theme.FromName(name);
            ThemeToken[] statuses = { ThemeToken.Pass, ThemeToken.Warn, ThemeToken.Fail };
            Color[] colours = statuses.Select(token => theme.Resolve(token, ColorDepth.TrueColor).Color).ToArray();
            double[] background = Linear(r, g, b);
            foreach (Color colour in colours) {
                Assert.True(Contrast(Linear(colour.R, colour.G, colour.B), background) >= 4.5, name + " " + colour + " contrast");
            }
            double[][][] simulations = {
                new[] { new[] { 0.152286, 1.052583, -0.204868 }, new[] { 0.114503, 0.786281, 0.099216 }, new[] { -0.003882, -0.048116, 1.051998 } },
                new[] { new[] { 0.367322, 0.860646, -0.227968 }, new[] { 0.280085, 0.672501, 0.047413 }, new[] { -0.011820, 0.042940, 0.968881 } }
            };
            foreach (double[][] matrix in simulations) {
                double[][] lab = colours.Select(colour => Lab(Simulate(matrix, Linear(colour.R, colour.G, colour.B)))).ToArray();
                for (int i = 0; i < lab.Length; i++) {
                    for (int j = i + 1; j < lab.Length; j++) {
                        double distance = Math.Sqrt(lab[i].Zip(lab[j], (x, y) => (x - y) * (x - y)).Sum());
                        Assert.True(distance >= 25, name + " " + statuses[i] + "/" + statuses[j] + " distance " + distance.ToString("0"));
                    }
                }
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

        private static double[] Linear(byte r, byte g, byte b) =>
            new[] { r, g, b }.Select(v => v / 255.0).Select(v => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4)).ToArray();

        private static double Luminance(double[] c) => 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];

        private static double Contrast(double[] a, double[] b) {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double[] Simulate(double[][] m, double[] c) =>
            m.Select(row => Math.Min(1, Math.Max(0, row[0] * c[0] + row[1] * c[1] + row[2] * c[2]))).ToArray();

        private static double[] Lab(double[] c) {
            double x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047, y = Luminance(c), z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883;
            Func<double, double> f = t => t > 0.008856 ? Math.Pow(t, 1.0 / 3) : 7.787 * t + 16.0 / 116;
            return new[] { 116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z)) };
        }
    }
}
