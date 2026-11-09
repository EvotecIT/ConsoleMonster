using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Serialization;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class SchemeThemeTests {
        public static IEnumerable<object[]> Catalogue() {
            using Stream stream = typeof(SchemeThemeTests).Assembly.GetManifestResourceStream("Colour-Schemes.catalogue.json")!;
            using StreamReader reader = new StreamReader(stream);
            IDictionary palettes = (IDictionary)MiniJson.Read(reader.ReadToEnd())!;
            foreach (DictionaryEntry entry in palettes) { yield return new[] { entry.Key }; }
        }
        private static TerminalScheme Scheme(string name) {
            using Stream stream = typeof(SchemeThemeTests).Assembly.GetManifestResourceStream("Colour-Schemes.catalogue.json")!;
            using StreamReader reader = new StreamReader(stream);
            IDictionary data = (IDictionary)((IDictionary)MiniJson.Read(reader.ReadToEnd())!)[name]!;
            Dictionary<string, object?> copy = new Dictionary<string, object?>();
            foreach (DictionaryEntry pair in data) { copy[(string)pair.Key] = pair.Value; }
            copy["name"] = name;
            return TerminalScheme.ParseWindowsTerminal(MiniJson.Write(copy));
        }

        [Theory]
        [MemberData(nameof(Catalogue))]
        public void FaithfulMappingUsesUnmodifiedCandidatesAndReadableMutedText(string name) {
            TerminalScheme scheme = Scheme(name);
            Theme theme = Theme.FromScheme(scheme);
            Assert.NotEqual(scheme.Background, theme.Get(ThemeToken.Muted).Color!.Value.TrueColor);
            Assert.Equal(scheme.Foreground, theme.Get(ThemeToken.Text).Color!.Value.TrueColor);
            Assert.Equal(scheme.Background, theme.Get(ThemeToken.Gap).Color!.Value.TrueColor);
            foreach ((ThemeToken token, int normal) in new[] { (ThemeToken.Pass, 2), (ThemeToken.Warn, 3), (ThemeToken.Fail, 1), (ThemeToken.Info, 6), (ThemeToken.Accent, 4), (ThemeToken.Accent2, 5) }) {
                Color color = theme.Get(token).Color!.Value.TrueColor;
                Assert.Contains(color, new[] { scheme.Palette[normal], scheme.Palette[normal + 8], scheme.Foreground });
                if (new[] { scheme.Palette[normal], scheme.Palette[normal + 8], scheme.Foreground }.Any(c => ThemeCheck.Contrast(c, scheme.Background) >= 3)) {
                    Assert.True(ThemeCheck.Contrast(color, scheme.Background) >= 3, name + " " + token);
                }
            }
            Assert.Empty(theme.AdjustedTokens);
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
                    : token == ThemeToken.Warn ? new[] { source.Palette[3], source.Palette[11] }
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
            return cosine > Math.Cos(3 * Math.PI / 180) && cb <= ca + .003;
        }

        [Fact]
        public void TerminalPresetKeepsPaletteReferencesAtTrueColorAndInFiles() {
            foreach (ColorDepth depth in new[] { ColorDepth.Ansi16, ColorDepth.Ansi256, ColorDepth.TrueColor }) {
                Assert.Equal(Color.FromAnsi16(2), Theme.Terminal.Resolve(ThemeToken.Pass, depth).Color);
                Assert.Equal(Color.FromAnsi16(1), Theme.Terminal.Resolve(ThemeToken.Fail, depth).Color);
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
