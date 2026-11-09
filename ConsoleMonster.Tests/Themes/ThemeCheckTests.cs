using System;
using System.Collections.Generic;
using ConsoleMonster.Interactive;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    public sealed class ThemeCheckTests {
        [Theory]
        [InlineData("Dark", 2)]
        [InlineData("ColorBlindSafeLight", 1)]
        public void PickerSummarisesTheHighlightedThemeInAtMostTwoLines(string name, int lineCount) {
            var values = new Dictionary<string, string> { ["Theme"] = name };
            string text = Terminal.RenderText(SettingsPicker.Preview(values), TerminalProfile.Fixed(60, 40, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            string summary = text.Substring(text.IndexOf("Statuses apart", StringComparison.Ordinal)).Trim();
            Assert.Equal(lineCount, summary.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length);
            Assert.Contains(name == "Dark" ? ": no" : ": yes", summary);
            if (name == "Dark") {
                Assert.Contains("pass and fail", summary);
                Assert.Contains("deuteranopia", summary);
                Assert.Contains("Pending text too faint: 4.0:1", summary);
            } else {
                Assert.DoesNotContain("too faint", summary);
            }
        }

        [Fact]
        public void LowContrastAndRedGreenStatusesFailWithoutBeingHiddenByMarkers() {
            Theme theme = Theme.ColorBlindSafe
                .With(ThemeToken.Text, new ThemeStyle(ThemeColor.Hex("#0c0c0c", 0)))
                .With(ThemeToken.Pass, new ThemeStyle(ThemeColor.Hex("#00ff00", 10)))
                .With(ThemeToken.Fail, new ThemeStyle(ThemeColor.Hex("#ff0000", 9)));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            ThemeCheckResult text = Assert.Single(report.Results, result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Text);
            Assert.Equal(ThemeCheckOutcome.Fail, text.Outcome);
            Assert.Equal(1.0, text.Value);
            Assert.Equal(4.5, text.Required);
            Assert.Contains(report.Results, result => result.Check == ThemeCheckKind.ColourVision && result.Outcome == ThemeCheckOutcome.Fail && result.Detail.Contains("deuteranopia"));
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
        }

        [Theory]
        [InlineData(ColorKind.Ansi16)]
        [InlineData(ColorKind.Ansi256)]
        [InlineData(ColorKind.Default)]
        public void MissingRgbIsUnknownWhileMarkersStillRun(ColorKind kind) {
            Color colour = kind == ColorKind.Ansi16 ? Color.FromAnsi16(2) : kind == ColorKind.Ansi256 ? Color.FromAnsi256(42) : Color.Default;
            Theme theme = Theme.ColorBlindSafe.With(ThemeToken.Pass, new ThemeStyle(new ThemeColor(colour, 2)));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.False(report.Passed);
            Assert.All(report.Results.Where(result => result.Tokens.Contains(ThemeToken.Pass) && result.Check != ThemeCheckKind.Markers), result => {
                Assert.Equal(ThemeCheckOutcome.Unknown, result.Outcome);
                Assert.Null(result.Value);
            });
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
        }

        [Fact]
        public void ContrastUsesTokenFillThenExplicitPageThenThemeVariant() {
            Theme theme = Theme.ColorBlindSafeLight.With(ThemeToken.Text, new ThemeStyle(ThemeColor.Hex("#000000", 0)));
            ThemeCheckReport light = ThemeCheck.Run(theme);
            Assert.Equal(21.0, light.Results.Single(result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Text).Value);
            ThemeCheckReport black = ThemeCheck.Run(theme, new ThemeCheckOptions { Background = Color.FromHex("#000000") });
            Assert.Equal(1.0, black.Results.Single(result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Text).Value);
            Assert.Equal(light.Results.Single(result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Critical).Value,
                black.Results.Single(result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Critical).Value);
        }

        [Fact]
        public void CriticalSharesFailHueButNeedsADistinctFillAndReadableText() {
            Theme theme = Theme.ColorBlindSafe;
            ThemeCheckResult fill = Assert.Single(ThemeCheck.Run(theme).Results, result => result.Check == ThemeCheckKind.Fill);
            Assert.Equal(ThemeCheckOutcome.Pass, fill.Outcome);
            Assert.Equal(theme.Get(ThemeToken.Fail).Color, theme.Get(ThemeToken.Critical).BackgroundColor);
            theme = theme.With(ThemeToken.Critical, new ThemeStyle(theme.Get(ThemeToken.Critical).Color, ThemeColor.Hex("#0c0c0c", 0)));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.Equal(ThemeCheckOutcome.Fail, report.Results.Single(result => result.Check == ThemeCheckKind.Fill).Outcome);
            Assert.Equal(ThemeCheckOutcome.Fail, report.Results.Single(result => result.Check == ThemeCheckKind.Contrast && result.Tokens[0] == ThemeToken.Critical).Outcome);
        }

        [Theory]
        [InlineData(ThemeToken.Text)]
        [InlineData(ThemeToken.Muted)]
        [InlineData(ThemeToken.Title)]
        [InlineData(ThemeToken.Header)]
        [InlineData(ThemeToken.Highlight)]
        [InlineData(ThemeToken.Strong)]
        public void CriticalFillMustDifferFromOrdinaryText(ThemeToken token) {
            Theme theme = Theme.ColorBlindSafe.With(token, Theme.ColorBlindSafe.Get(ThemeToken.Critical));
            Assert.Equal(ThemeCheckOutcome.Fail, ThemeCheck.Run(theme).Results.Single(result => result.Check == ThemeCheckKind.Fill).Outcome);
        }

        [Fact]
        public void KnownFillFailureIsNotMaskedByAnotherUnknownBackground() {
            Theme theme = Theme.ColorBlindSafe
                .With(ThemeToken.Pass, new ThemeStyle(Theme.ColorBlindSafe.Get(ThemeToken.Pass).Color, new ThemeColor(Color.FromAnsi16(0), 0)))
                .With(ThemeToken.Critical, new ThemeStyle(ThemeColor.Hex("#ffffff", 15), ThemeColor.Hex("#0c0c0c", 0)));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.Contains(report.Results, result => result.Check == ThemeCheckKind.Contrast && result.Outcome == ThemeCheckOutcome.Unknown);
            Assert.Equal(ThemeCheckOutcome.Fail, report.Results.Single(result => result.Check == ThemeCheckKind.Fill).Outcome);
            Assert.DoesNotContain(report.Results, result => result.Check != ThemeCheckKind.Fill && result.Outcome == ThemeCheckOutcome.Fail);
        }

        [Fact]
        public void DimTextCannotClaimUndimmedContrastOrStatusDistances() {
            Theme theme = Theme.ColorBlindSafe
                .With(ThemeToken.Text, new ThemeStyle(ThemeColor.Hex("#aaaaaa", 7), null, TextDecoration.Dim))
                .With(ThemeToken.Pass, new ThemeStyle(Theme.ColorBlindSafe.Get(ThemeToken.Pass).Color, null, TextDecoration.Dim));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Contrast && (result.Tokens[0] == ThemeToken.Text || result.Tokens[0] == ThemeToken.Pass)), result => {
                Assert.Equal(ThemeCheckOutcome.Unknown, result.Outcome);
                Assert.Null(result.Value);
            });
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision && result.Tokens.Contains(ThemeToken.Pass)), result => Assert.Equal(ThemeCheckOutcome.Unknown, result.Outcome));
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
        }

        [Fact]
        public void ReversedStatusesUseTheDisplayedTextAndFill() {
            Theme theme = Theme.ColorBlindSafe;
            ThemeStyle pass = theme.Get(ThemeToken.Pass);
            ThemeStyle fail = theme.Get(ThemeToken.Fail);
            theme = theme.With(ThemeToken.Pass, new ThemeStyle(pass.Color, fail.Color, TextDecoration.Reverse));
            ThemeCheckReport report = ThemeCheck.Run(theme);
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision && result.Tokens.Contains(ThemeToken.Pass) && result.Tokens.Contains(ThemeToken.Fail)), result => {
                Assert.Equal(ThemeCheckOutcome.Fail, result.Outcome);
                Assert.Equal(0.0, result.Value);
            });
        }

        [Theory]
        [InlineData(GlyphTier.Full, StatusMarkerStyle.Symbols)]
        [InlineData(GlyphTier.Basic, StatusMarkerStyle.Symbols)]
        [InlineData(GlyphTier.Ascii, StatusMarkerStyle.Symbols)]
        [InlineData(GlyphTier.Full, StatusMarkerStyle.Words)]
        public void MarkersUseTheChosenRenderingConditions(GlyphTier tier, StatusMarkerStyle markers) {
            ThemeCheckReport report = ThemeCheck.Run(Theme.HighContrast, new ThemeCheckOptions { GlyphTier = tier, StatusMarkers = markers });
            Assert.All(report.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Pass, result.Outcome));
            ThemeCheckReport unavailable = ThemeCheck.Run(Theme.HighContrast, new ThemeCheckOptions { GlyphTier = null });
            Assert.All(unavailable.Results.Where(result => result.Check == ThemeCheckKind.Markers), result => Assert.Equal(ThemeCheckOutcome.Unknown, result.Outcome));
        }
    }
}
