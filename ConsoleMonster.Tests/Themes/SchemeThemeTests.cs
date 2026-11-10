using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class SchemeThemeTests {
        public static IEnumerable<object[]> Catalogue() {
            foreach (ThemeScheme entry in Theme.Schemes) { yield return new object[] { entry.Name }; }
        }
        private static TerminalScheme Scheme(string name) => Theme.Schemes.Single(entry => entry.Name == name).Scheme;

        [Theory]
        [MemberData(nameof(Catalogue))]
        public void FaithfulMappingUsesUnmodifiedCandidatesAndReadableMutedText(string name) {
            TerminalScheme scheme = Scheme(name);
            Theme theme = Theme.FromScheme(scheme);
            Assert.NotEqual(scheme.Background, theme.Get(ThemeToken.Muted).Color!.Value.TrueColor);
            Assert.Equal(scheme.Foreground, theme.Get(ThemeToken.Text).Color!.Value.TrueColor);
            Assert.Equal(theme.Get(ThemeToken.Muted), theme.Get(ThemeToken.Gap));
            Assert.NotEqual(scheme.Background, theme.Get(ThemeToken.Gap).Color!.Value.TrueColor);
            foreach ((ThemeToken token, int normal) in new[] { (ThemeToken.Pass, 2), (ThemeToken.Warn, 3), (ThemeToken.Fail, 1), (ThemeToken.Info, 6), (ThemeToken.Accent, 4), (ThemeToken.Accent2, 5) }) {
                Color color = theme.Get(token).Color!.Value.TrueColor;
                if (token == ThemeToken.Accent && scheme.SignatureAccent.HasValue) { Assert.Equal(scheme.SignatureAccent.Value, color); continue; }
                Assert.Contains(color, new[] { scheme.Palette[normal], scheme.Palette[normal + 8] });
                SchemeLightness basis = new SchemeLightness(scheme.Palette[normal]), bright = new SchemeLightness(scheme.Palette[normal + 8]);
                double hue = Math.Abs(basis.Hue - bright.Hue);
                bool eligible = bright.Chroma >= .05 && Math.Min(hue, 360 - hue) <= 30;
                if (ThemeCheck.Contrast(scheme.Palette[normal], scheme.Background) >= 3 || (eligible && ThemeCheck.Contrast(scheme.Palette[normal + 8], scheme.Background) >= 3)) {
                    Assert.True(ThemeCheck.Contrast(color, scheme.Background) >= 3, name + " " + token);
                }
            }
            Assert.Empty(theme.AdjustedTokens);
        }

        [Fact]
        public void GapUsesTheHalfwayMutedFallbackWhenPaletteGreysAreTooFaint() {
            Color background = Color.FromHex("#000000");
            TerminalScheme scheme = new TerminalScheme("Muted fallback", Enumerable.Repeat(background, 16).ToArray(), Color.FromHex("#ffffff"), background);
            Theme theme = Theme.FromScheme(scheme);
            Assert.Equal(Color.FromHex("#808080"), theme.Get(ThemeToken.Gap).Color!.Value.TrueColor);
            Assert.Equal(theme.Get(ThemeToken.Muted), theme.Get(ThemeToken.Gap));
            Assert.True(ThemeCheck.Contrast(theme.Get(ThemeToken.Gap).Color!.Value.TrueColor, background) >= 3);
        }

        [Fact]
        public void AccessibleWarningUsesPublishedOrangeWhenYellowCannotKeepItsChroma() {
            TerminalScheme source = Scheme("Solarized Light");
            Color[] palette = source.Palette.ToArray();
            palette[3] = palette[11] = Color.FromHex("#ffff00");
            Color background = Color.FromHex("#b0b0b0");
            Theme theme = Theme.FromScheme(new TerminalScheme("Orange fallback", palette, source.Foreground, background), true);
            Color warning = theme.Get(ThemeToken.Warn).Color!.Value.TrueColor;
            Assert.True(ThemeCheck.Run(theme).Passed);
            Assert.True(SameHue(source.Palette[9], warning));
            Assert.InRange(new SchemeLightness(warning).Hue, 30, 60);
            palette[9] = palette[3];
            Theme withoutOrange = Theme.FromScheme(new TerminalScheme("No orange", palette, source.Foreground, background), true);
            Assert.Contains(ThemeCheck.Run(withoutOrange).Results, result => result.Tokens.Contains(ThemeToken.Warn)
                && (result.Check == ThemeCheckKind.Contrast || result.Check == ThemeCheckKind.ColourVision) && result.Outcome == ThemeCheckOutcome.Fail);
        }

        [Fact]
        public void SolarizedLightKeepsFaithfulStatusHuesAndAccessibleWarningChroma() {
            TerminalScheme scheme = Scheme("Solarized Light");
            Theme faithful = Theme.FromScheme(scheme);
            Assert.Equal(scheme.Palette[2], faithful.Get(ThemeToken.Pass).Color!.Value.TrueColor);
            Assert.Equal(scheme.Palette[3], faithful.Get(ThemeToken.Warn).Color!.Value.TrueColor);
            Assert.InRange(new SchemeLightness(faithful.Get(ThemeToken.Pass).Color!.Value.TrueColor).Hue, 110, 160);
            Assert.InRange(new SchemeLightness(faithful.Get(ThemeToken.Warn).Color!.Value.TrueColor).Hue, 65, 115);
            Theme accessible = Theme.FromScheme(scheme, true);
            SchemeLightness warning = new SchemeLightness(accessible.Get(ThemeToken.Warn).Color!.Value.TrueColor);
            Assert.InRange(warning.Hue, 30, 120);
            Assert.True(warning.Chroma >= new SchemeLightness(scheme.Palette[3]).Chroma * .5, "Warning lost its colour: " + warning.Chroma);
            Assert.True(ThemeCheck.Run(accessible).Passed);
        }

        [Theory]
        [MemberData(nameof(Catalogue))]
        public void AccessibleVariantPassesEveryThemeCheckAndRoundTripsWithOverrides(string name) {
            Theme theme = Theme.FromScheme(Scheme(name), true);
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.True(report.Passed, name + ": " + string.Join("; ", report.Results.Where(r => r.Outcome != ThemeCheckOutcome.Pass).Select(r => r.Detail + " " + r.Value)));
            TerminalScheme source = theme.Scheme!;
            Theme faithful = Theme.FromScheme(source);
            foreach (ThemeToken token in theme.AdjustedTokens) {
                ThemeStyle actual = theme.Get(token);
                Color target = token == ThemeToken.Critical ? actual.BackgroundColor!.Value.TrueColor : actual.Color!.Value.TrueColor;
                IEnumerable<Color> originals = token == ThemeToken.Pass ? new[] { source.Palette[4], source.Palette[12], source.Palette[6], source.Palette[14] }
                    : token == ThemeToken.Warn ? source.Palette.Where(c => { SchemeLightness lch = new SchemeLightness(c); return lch.Chroma >= .05 && lch.Hue >= 30 && lch.Hue <= 120; })
                    : token == ThemeToken.Fail ? new[] { source.Palette[1], source.Palette[9], source.Palette[5], source.Palette[13] }
                    : new[] { token == ThemeToken.Critical ? faithful.Get(token).BackgroundColor!.Value.TrueColor : faithful.Get(token).Color!.Value.TrueColor };
                Assert.True(originals.Any(original => SameHue(original, target)), name + " changed hue for " + token);
            }
            Assert.True((theme.Get(ThemeToken.Fail).Decoration & TextDecoration.Underline) != 0);
            Theme overridden = theme.With(ThemeToken.Accent, new ThemeStyle(ThemeColor.Hex("#123456", 4))).Rename("Team");
            string json = ThemeFile.FromTheme(overridden).ToJson();
            Assert.Contains("\"SchemaVersion\":3", json.Replace(" ", ""));
            Theme loaded = ThemeFile.Parse(json).ToTheme();
            Assert.NotNull(loaded.Scheme);
            Assert.True(loaded.IsAccessible);
            Assert.Equal(theme.AdjustedTokens, loaded.AdjustedTokens);
            foreach (ThemeToken token in Theme.AllTokens()) { Assert.Equal(overridden.Get(token), loaded.Get(token)); }
        }

        [Fact]
        public void AccessibleConversionPreservesOnlyExplicitFieldsIncludingValuesEqualToTheBasis() {
            TerminalScheme scheme = Scheme("Dracula");
            Theme faithful = Theme.FromScheme(scheme);
            IDictionary<string, object?> data = ThemeFile.FromTheme(faithful).ToData();
            data["Tokens"] = new Dictionary<string, object?> { ["Pass"] = new Dictionary<string, object?> { ["Decoration"] = "Bold" } };
            ThemeFile partial = ThemeFile.Import((IDictionary)data);
            Theme accessible = ThemeFile.Parse(partial.ToJson()).ToTheme(true);
            Assert.Equal(Theme.FromScheme(scheme, true).Get(ThemeToken.Pass).Color, accessible.Get(ThemeToken.Pass).Color);
            Assert.Equal(TextDecoration.Bold, accessible.Get(ThemeToken.Pass).Decoration);
            Color exact = faithful.Get(ThemeToken.Pass).Color!.Value.TrueColor;
            data["Tokens"] = new Dictionary<string, object?> { ["Pass"] = new Dictionary<string, object?> { ["Foreground"] = exact.ToString() },
                ["Focus"] = new Dictionary<string, object?> { ["Background"] = null } };
            ThemeFile explicitColor = ThemeFile.Import((IDictionary)data);
            Theme roundtrip = ThemeFile.Parse(ThemeFile.FromTheme(explicitColor.ToTheme()).ToJson()).ToTheme(true);
            Assert.Equal(exact, roundtrip.Get(ThemeToken.Pass).Color!.Value.TrueColor);
            Assert.Null(roundtrip.Get(ThemeToken.Focus).BackgroundColor);
        }

        private static bool SameHue(Color original, Color adjusted) {
            SchemeLightness a = new SchemeLightness(original), b = new SchemeLightness(adjusted);
            double ca = Math.Sqrt(a.A * a.A + a.B * a.B), cb = Math.Sqrt(b.A * b.A + b.B * b.B);
            if (ca < .001 || cb < .001) { return cb <= ca + .003; }
            double cosine = (a.A * b.A + a.B * b.B) / (ca * cb);
            return cosine > Math.Cos(3 * Math.PI / 180) && cb <= ca + .003 && cb >= ca * .5;
        }

        [Fact]
        public void TerminalPresetKeepsPaletteReferencesAtTrueColorAndInFiles() {
            foreach (ColorDepth depth in new[] { ColorDepth.Ansi16, ColorDepth.Ansi256, ColorDepth.TrueColor }) {
                Assert.Equal(Color.FromAnsi16(2), Theme.Terminal.Resolve(ThemeToken.Pass, depth).Color);
                Assert.Equal(Color.FromAnsi16(1), Theme.Terminal.Resolve(ThemeToken.Fail, depth).Color);
                Assert.Equal(Theme.Terminal.Resolve(ThemeToken.Muted, depth), Theme.Terminal.Resolve(ThemeToken.Gap, depth));
            }
            Theme actual = ThemeFile.Parse("{\"SchemaVersion\":3,\"Name\":\"Terminal\",\"BaseTheme\":\"Terminal\",\"Tokens\":{}}").ToTheme();
            Assert.True(actual.FollowsTerminal);
            Assert.True(actual.Get(ThemeToken.Pass).Color!.Value.IsPaletteIndex);
            Theme indexed = Theme.Dark.With(ThemeToken.Pass, new ThemeStyle(ThemeColor.PaletteIndex(2)));
            Theme copied = ThemeFile.Parse(ThemeFile.FromTheme(indexed).ToJson()).ToTheme();
            Assert.True(copied.Get(ThemeToken.Pass).Color!.Value.IsPaletteIndex);
            Assert.Equal(Color.FromAnsi16(2), actual.Resolve(ThemeToken.Pass, ColorDepth.TrueColor).Color);
        }
    }
}
