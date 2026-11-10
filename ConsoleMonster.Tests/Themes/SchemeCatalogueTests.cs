using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class SchemeCatalogueTests {
        [Fact]
        public void ShippedCatalogueRoundTripsBothFormsAndRecordsFaithfulMeasurements() {
            StringBuilder measurements = new StringBuilder();
            foreach (ThemeScheme entry in Theme.Schemes) {
                Assert.Equal(entry.Name, entry.Scheme.Name);
                Assert.False(string.IsNullOrWhiteSpace(entry.Family));
                Assert.False(string.IsNullOrWhiteSpace(entry.DisplayName));
                foreach (bool accessible in new[] { false, true }) {
                    string choice = entry.Name + (accessible ? " · accessible" : "");
                    Theme named = Theme.FromName(choice);
                    Assert.Equal(entry.Variant, named.Appearance.Background);
                    Assert.Equal(entry.Scheme.Palette, named.Scheme!.Palette);
                    Theme imported = ThemeFile.Parse("{\"SchemaVersion\":3,\"Name\":\"Team\",\"BaseScheme\":\"" + entry.Name
                        + "\"" + (accessible ? ",\"Variant\":\"Accessible\"" : "") + "}").ToTheme();
                    Theme copy = ThemeFile.Parse(ThemeFile.FromTheme(imported).ToJson()).ToTheme();
                    Assert.Equal(accessible, copy.IsAccessible);
                    Assert.Equal(entry.Scheme.Palette, copy.Scheme!.Palette);
                    Assert.Equal(entry.Scheme.Selection, copy.Scheme.Selection);
                    Assert.Equal(entry.Scheme.Cursor, copy.Scheme.Cursor);
                    Assert.Equal(entry.Scheme.SignatureAccent, copy.Scheme.SignatureAccent);
                    foreach (ThemeToken token in Theme.AllTokens()) {
                        Assert.Equal(named.Get(token), imported.Get(token));
                        Assert.Equal(imported.Get(token), copy.Get(token));
                    }
                    ThemeCheckReport report = ThemeCheck.Run(copy);
                    if (accessible) { Assert.True(report.Passed, entry.Name + ": " + string.Join("; ", report.Results.Where(r => r.Outcome != ThemeCheckOutcome.Pass).Select(r => r.Detail))); }
                    else {
                        if (measurements.Length > 0) { measurements.AppendLine(); }
                        measurements.AppendLine(entry.Name);
                        foreach (ThemeCheckResult result in report.Results) {
                            measurements.AppendLine(string.Join(" | ", result.Check, string.Join(",", result.Tokens), result.Vision ?? "-", result.Outcome,
                                result.Value?.ToString("F3", CultureInfo.InvariantCulture) ?? "-", result.Required?.ToString("F3", CultureInfo.InvariantCulture) ?? "-"));
                        }
                    }
                }
            }
            using Stream stream = typeof(SchemeCatalogueTests).Assembly.GetManifestResourceStream("Catalogue.faithful.txt")!;
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(reader.ReadToEnd().Replace("\r\n", "\n"), measurements.ToString().Replace("\r\n", "\n"));
        }

        [Theory]
        [InlineData("Dark")]
        [InlineData("Unknown scheme")]
        [InlineData("Dracula · accessible")]
        public void NamedSchemeReferencesRejectPresetsUnknownNamesAndSuffixes(string name) {
            FormatException error = Assert.Throws<FormatException>(() => ThemeFile.Parse("{\"SchemaVersion\":3,\"Name\":\"Team\",\"BaseScheme\":\"" + name + "\"}"));
            Assert.Contains("BaseScheme", error.Message);
        }
    }
}
