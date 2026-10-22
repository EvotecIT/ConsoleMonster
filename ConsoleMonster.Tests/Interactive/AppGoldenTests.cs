using System;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Golden endpoints from the compiled live-filter app, using its actual keyboard and live rendering engine.</summary>
    [Collection("Live sessions")]
    public sealed class AppGoldenTests {
        /// <summary>The same example stays readable at desktop and compact widths, including ASCII/no-colour output.</summary>
        [Theory]
        [InlineData(120, GlyphTier.Full)]
        [InlineData(60, GlyphTier.Full)]
        [InlineData(60, GlyphTier.Ascii)]
        public void LiveFilter_InitialAndFilteredFramesMatchGoldens(int width, GlyphTier tier) {
            ScreenCapture capture = Capture(width, tier, ColorDepth.None, Theme.Dark);
            string key = "app.live-filter." + width + "." + tier.ToString().ToLowerInvariant();
            Golden.Match(key + ".initial.txt", capture.Frames[0].ToPlainText());
            Golden.Match(key + ".filtered.txt", capture.LastFrame!.ToPlainText());
            Assert.Contains("6 of 6 hosts", capture.Frames[0].ToPlainText());
            Assert.Contains("2 of 6 hosts", capture.LastFrame.ToPlainText());
            Assert.Contains("SQL01", capture.LastFrame.ToPlainText());
            Assert.DoesNotContain("WEB01", capture.LastFrame.ToPlainText());
        }

        /// <summary>Focus uses the active theme's accent at both supported colour tiers, and marks the caret independently.</summary>
        [Theory]
        [InlineData(false, ColorDepth.Ansi16)]
        [InlineData(true, ColorDepth.Ansi16)]
        [InlineData(false, ColorDepth.TrueColor)]
        [InlineData(true, ColorDepth.TrueColor)]
        public void LiveFilter_FocusAndCaretUseActiveTheme(bool light, ColorDepth depth) {
            Theme theme = light ? Theme.Light : Theme.Dark;
            ScreenCapture capture = Capture(60, GlyphTier.Full, depth, theme);
            Assert.Equal(theme.Resolve(ThemeToken.Accent, depth).Foreground, capture.LastFrame![0, 2].Style.Foreground);
            Assert.True((capture.LastFrame[5, 3].Style.Decoration & TextDecoration.Reverse) != 0);
        }

        private static ScreenCapture Capture(int width, GlyphTier tier, ColorDepth depth, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(width, 24);
            using (ScriptedInput input = new ScriptedInput("\"sql\" Escape")) {
                AppState state = LiveFilterApp.Run(new InteractiveOptions {
                    Input = input, Capture = capture, Theme = theme, Mode = InteractionMode.Rich,
                    Profile = TerminalProfile.Fixed(width, 24, depth, tier).WithCulture(CultureInfo.GetCultureInfo("en-US"))
                });
                Assert.Equal("sql", state["Query"]);
            }
            return capture;
        }
    }
}
