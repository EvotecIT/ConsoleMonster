using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class ThemeFileTests {
        [Theory]
        [InlineData("Dark")]
        [InlineData("Light")]
        [InlineData("HighContrast")]
        [InlineData("ColorBlindSafe")]
        public void Json_RoundTripsEveryTokenDefinitionAndColourTier(string preset) {
            Theme original = Theme.FromName(preset).Rename("Team's theme – 日本語");
            Theme actual = ThemeFile.Parse(ThemeFile.FromTheme(original).ToJson()).ToTheme();
            Assert.Equal(original.Name, actual.Name);
            EqualDefinitions(original, actual);
        }

        [Fact]
        public void Json_PreservesExplicitAndAutomaticFallbacksAndAllDecorations() {
            TextDecoration all = TextDecoration.Bold | TextDecoration.Dim | TextDecoration.Italic | TextDecoration.Underline
                | TextDecoration.Strikethrough | TextDecoration.Reverse;
            Theme original = Theme.Dark
                .With(ThemeToken.Text, new ThemeStyle(new ThemeColor(Color.Default, -1, -1)))
                .With(ThemeToken.Accent, new ThemeStyle(new ThemeColor(Color.FromAnsi16(3), 15, 255),
                    new ThemeColor(Color.FromAnsi256(16), 0), all))
                .With(ThemeToken.Border, new ThemeStyle(ThemeColor.Hex("#012345", -1, -1)));
            EqualDefinitions(original, ThemeFile.Parse(ThemeFile.FromTheme(original).ToJson()).ToTheme());
        }

        [Fact]
        public void Import_InheritsMissingTokensAndStyleFields_AndNullClearsAColour() {
            Hashtable tokens = new Hashtable {
                ["surfaceaccent"] = new Hashtable { ["foreground"] = "#123456", ["decoration"] = "None" },
                ["Focus"] = new Hashtable { ["Background"] = null }
            };
            Hashtable data = new Hashtable { ["name"] = "Team", ["basetheme"] = "Light", ["tokens"] = tokens };
            ThemeFile document = ThemeFile.Import(data);
            tokens.Clear();
            Theme actual = document.ToTheme();
            Assert.Equal("Team", actual.Name);
            Assert.Equal(Theme.Light.Get(ThemeToken.SurfaceAccent).Background, actual.Get(ThemeToken.SurfaceAccent).Background);
            Assert.Equal(Color.FromHex("#123456"), actual.Get(ThemeToken.SurfaceAccent).Foreground!.Value.TrueColor);
            Assert.Equal(TextDecoration.None, actual.Get(ThemeToken.SurfaceAccent).Decoration);
            Assert.Null(actual.Get(ThemeToken.Focus).Background);
            Assert.Equal(Theme.Light.Get(ThemeToken.Pass), actual.Get(ThemeToken.Pass));
            Assert.DoesNotContain("BaseTheme", document.ToJson());
        }

        [Fact]
        public void Import_WithoutBaseTheme_UsesTerminalDefaultsForOmissions() {
            Theme theme = ThemeFile.Import(Document(new Hashtable { ["Pass"] = new Hashtable { ["Decoration"] = "Bold" } })).ToTheme();
            Assert.Equal(Style.Plain, theme.Resolve(ThemeToken.Text, ColorDepth.TrueColor));
            Assert.Null(theme.Get(ThemeToken.Pass).Foreground);
            Assert.Equal(TextDecoration.Bold, theme.Get(ThemeToken.Pass).Decoration);
        }

        /// <summary>JSON preserves scalar types before applying the same string validation as PSD1 data dictionaries.</summary>
        [Theory]
        [InlineData("{\"Name\":123,\"Tokens\":{}}", "Name")]
        [InlineData("{\"Name\":\"Team\",\"Tokens\":{\"Accent\":{\"Foreground\":208}}}", "Tokens.Accent.Foreground")]
        [InlineData("{\"Name\":\"Team\",\"Tokens\":{\"Accent\":{\"Foreground\":{\"TrueColor\":208}}}}", "Tokens.Accent.Foreground.TrueColor")]
        [InlineData("{\"Name\":\"Team\",\"Tokens\":{\"Accent\":{\"Decoration\":1}}}", "Tokens.Accent.Decoration")]
        public void Parse_RejectsNumericValuesInStringFields(string json, string location) {
            Assert.Contains(location, Assert.Throws<FormatException>(() => ThemeFile.Parse(json)).Message);
        }

        [Theory]
        [InlineData("Tokens.Unknown", "token")]
        [InlineData("Tokens.Pass.Colour", "key")]
        [InlineData("Tokens.Pass.Foreground", "colour")]
        [InlineData("Tokens.Pass.Decoration", "decoration")]
        [InlineData("BaseTheme", "preset")]
        [InlineData("Theme.Extra", "root")]
        [InlineData("Tokens.Pass.Foreground.Ansi16", "low")]
        [InlineData("Tokens.Pass.Foreground.Ansi256", "middle")]
        [InlineData("SchemaVersion", "version")]
        public void Import_RejectsBadDataWithItsLocation(string location, string problem) {
            Hashtable style = new Hashtable();
            Hashtable tokens = new Hashtable { ["Pass"] = style };
            Hashtable data = Document(tokens);
            switch (problem) {
                case "token": tokens["Unknown"] = new Hashtable(); break;
                case "key": style["Colour"] = "red"; break;
                case "colour": style["Foreground"] = "#gg0000"; break;
                case "decoration": style["Decoration"] = "64"; break;
                case "preset": data["BaseTheme"] = "Dakr"; break;
                case "root": data["Extra"] = "ignored?"; break;
                case "low": style["Foreground"] = new Hashtable { ["TrueColor"] = "#fff", ["Ansi16"] = 16 }; break;
                case "middle": style["Foreground"] = new Hashtable { ["TrueColor"] = "#fff", ["Ansi256"] = -2 }; break;
                case "version": data["SchemaVersion"] = 2; break;
            }
            FormatException error = Assert.Throws<FormatException>(() => ThemeFile.Import(data));
            Assert.Contains(location, error.Message);
        }

        [Theory]
        [InlineData(-2, -1)]
        [InlineData(16, -1)]
        [InlineData(1, -2)]
        [InlineData(1, 256)]
        public void Export_ValidatesProgrammaticFallbackIndices(int ansi16, int ansi256) {
            Theme invalid = Theme.Dark.With(ThemeToken.Accent, new ThemeStyle(ThemeColor.Hex("#123456", ansi16, ansi256)));
            Assert.Throws<FormatException>(() => ThemeFile.FromTheme(invalid));
        }

        [Fact]
        public void Import_RejectsDuplicateCaseAndWhitespaceTokenNames() {
            Dictionary<string, object?> tokens = new Dictionary<string, object?> {
                ["Pass"] = new Hashtable(), [" pass "] = new Hashtable()
            };
            Assert.Contains("duplicate", Assert.Throws<FormatException>(() => ThemeFile.Import(Document(tokens))).Message);
            Assert.Contains("duplicate", Assert.Throws<FormatException>(() => ThemeFile.Parse("{\"Name\":\"one\",\"name\":\"two\",\"Tokens\":{}}" )).Message);
        }

        [Fact]
        public void SaveAndLoad_UseUtf8AndDoNotMutateTheActiveTheme() {
            string path = Path.Combine(Path.GetTempPath(), "consolemonster-theme-" + Guid.NewGuid().ToString("N") + ".json");
            ITheme before = Terminal.Theme;
            try {
                ThemeFile document = ThemeFile.FromTheme(Theme.Light.Rename("Zażółć – テーマ"));
                document.Save(path);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal((byte)'{', bytes[0]);
                Assert.Equal(document.Name, ThemeFile.Load(path).Name);
                EqualDefinitions(document.ToTheme(), ThemeFile.Load(path).ToTheme());
                Assert.Same(before, Terminal.Theme);
            } finally {
                if (File.Exists(path)) { File.Delete(path); }
            }
        }

        [Fact]
        public void Export_CustomTheme_CapturesTiers_OrReportsUnrepresentableStyles() {
            EqualDefinitions(Theme.Light, ThemeFile.FromTheme(new DelegatingTheme(false)).ToTheme());
            ArgumentException error = Assert.Throws<ArgumentException>(() => ThemeFile.FromTheme(new DelegatingTheme(true)));
            Assert.Contains("losslessly", error.Message);
        }

        private static Hashtable Document(object tokens) => new Hashtable { ["Name"] = "Team", ["Tokens"] = tokens };

        private static void EqualDefinitions(Theme expected, Theme actual) {
            foreach (ThemeToken token in Enum.GetValues(typeof(ThemeToken))) {
                ThemeStyle left = expected.Get(token);
                ThemeStyle right = actual.Get(token);
                Assert.Equal(left.Foreground, right.Foreground);
                Assert.Equal(left.Background, right.Background);
                Assert.Equal(left.Decoration, right.Decoration);
                foreach (ColorDepth depth in Enum.GetValues(typeof(ColorDepth))) {
                    Assert.Equal(expected.Resolve(token, depth), actual.Resolve(token, depth));
                }
            }
        }

        private sealed class DelegatingTheme : ITheme {
            private readonly bool _tierDecoration;
            internal DelegatingTheme(bool tierDecoration) { _tierDecoration = tierDecoration; }
            public string Name => "Custom";
            public Style Resolve(ThemeToken token, ColorDepth depth) => _tierDecoration && depth == ColorDepth.Ansi16
                ? Theme.Light.Resolve(token, depth).With(TextDecoration.Italic) : Theme.Light.Resolve(token, depth);
            public bool TryResolve(string name, ColorDepth depth, out Style style) => Theme.Light.TryResolve(name, depth, out style);
        }
    }
}
