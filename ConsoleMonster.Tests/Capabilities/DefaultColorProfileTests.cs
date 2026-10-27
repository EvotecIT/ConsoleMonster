using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    public sealed class DefaultColorProfileTests {
        [Fact]
        public void WithDefaultColors_AllProfileCopies_KeepBaselineAndOriginalProfileUnchanged() {
            TerminalProfile original = TerminalProfile.Fixed(80, 24);
            Color foreground = Color.FromConsoleColor(ConsoleColor.Black);
            Color background = Color.FromConsoleColor(ConsoleColor.White);
            TerminalProfile profile = original.WithDefaultColors(foreground, background)
                .WithCulture(CultureInfo.GetCultureInfo("pl-PL")).WithLanguage(CultureInfo.GetCultureInfo("de-DE"));
            TerminalProfile[] copies = {
                profile.WithColorDepth(ColorDepth.Ansi16), profile.WithGlyphs(GlyphTier.Basic),
                profile.WithMode(OutputMode.Stream), profile.WithThemePreference(ThemePreference.Light),
                profile.WithCulture(null), profile.WithLanguage(null), profile.WithSize(120, 40),
                profile.WithDecision("baseline retained"), profile.WithWidthProfile(TerminalWidthProfile.Standard),
                profile.WithHyperlinks(true), new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string>()))
                    .RefreshFont(profile, "Consolas")
            };
            foreach (TerminalProfile copy in copies) {
                Assert.Equal(foreground, copy.DefaultForeground);
                Assert.Equal(background, copy.DefaultBackground);
            }
            Assert.True(original.DefaultForeground.IsDefault);
            Assert.True(original.DefaultBackground.IsDefault);
            Assert.Equal("pl-PL", profile.Culture!.Name);
            Assert.Equal("de-DE", profile.Language!.Name);
            TerminalProfile cleared = profile.WithDefaultColors(Color.Default, Color.Default);
            Assert.True(cleared.DefaultForeground.IsDefault);
            Assert.True(cleared.DefaultBackground.IsDefault);
            Assert.Equal(background, profile.DefaultBackground);
        }

        [Fact]
        public void FromConsoleColor_All16Colors_RoundTripThroughCanonicalPalette() {
            ConsoleColorMap map = new ConsoleColorMap(Terminal.CreateContext(TerminalProfile.Fixed(80, 24), Theme.Light));
            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor))) {
                Color converted = Color.FromConsoleColor(color);
                Assert.Equal(ColorKind.Ansi16, converted.Kind);
                Assert.Equal(color, map.Background(converted));
            }
            Assert.Equal(Color.FromAnsi16(4), Color.FromConsoleColor(ConsoleColor.DarkBlue));
            Assert.Equal(Color.FromAnsi16(15), Color.FromConsoleColor(ConsoleColor.White));
        }

        [Fact]
        public void OriginalSignatures_CompiledConsumerContracts_RemainAvailableWithDefaultBytes() {
            Type[] constructor = {
                typeof(ColorDepth), typeof(GlyphTier), typeof(OutputMode), typeof(int), typeof(int), typeof(TerminalKind),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(IReadOnlyList<string>),
                typeof(TerminalWidthProfile), typeof(bool), typeof(ThemePreference), typeof(CultureInfo)
            };
            Assert.NotNull(typeof(TerminalProfile).GetConstructor(constructor));
            List<Type> current = new List<Type>(constructor) { typeof(bool?) };
            Assert.NotNull(typeof(TerminalProfile).GetConstructor(current.ToArray()));
            Assert.NotNull(typeof(SgrEncoder).GetMethod("Append", new[] { typeof(StringBuilder), typeof(Style), typeof(ColorDepth) }));
            StringBuilder builder = new StringBuilder();
            SgrEncoder.Append(builder, new Style(Color.FromAnsi16(1), Color.FromAnsi16(4), TextDecoration.Bold), ColorDepth.Ansi16);
            Assert.Equal("\u001b[0;1;31;44m", builder.ToString());
        }
    }
}
