using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Cell presenters and tone rules: culture, units, relative dates, tiers and readability without colour.</summary>
    public sealed class PresenterTests {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private static PresenterContext Context(GlyphTier tier = GlyphTier.Full, ColorDepth depth = ColorDepth.TrueColor, string culture = "", TerminalKind kind = TerminalKind.Unknown) {
            TerminalProfile profile = new TerminalProfile(depth, tier, OutputMode.Interactive, 80, 24, kind, supportsVirtualTerminal: true);
            return new PresenterContext(new RenderContext(profile, Theme.Dark), CultureInfo.GetCultureInfo(culture), Now, TimeZoneInfo.Utc);
        }

        [Theory]
        [InlineData("", 1234567.891, "1,234,567.89")]
        [InlineData("pl-PL", 1234567.891, "1 234 567,89")]
        [InlineData("de-DE", 12480, "12.480")]
        public void Number_Culture_UsesThousandAndDecimalSeparators(string culture, double value, string expected) {
            PresentedValue presented = CellPresenters.Number.Present(value, Context(culture: culture));
            Assert.Equal(expected, presented.Text);
            Assert.Equal(Layout.HorizontalAlignment.Right, presented.Alignment);
        }

        [Fact]
        public void Number_Unit_FollowsAfterASpace() {
            Assert.Equal("8.4 rules/s", new NumberPresenter("rules/s").Present(8.4, Context()).Text);
        }

        [Theory]
        [InlineData(512L, "512 B")]
        [InlineData(1536L, "1.5 KB")]
        [InlineData(1482113L, "1.41 MB")]
        [InlineData(5368709120L, "5 GB")]
        public void Bytes_BinaryUnits_Format(long value, string expected) {
            Assert.Equal(expected, CellPresenters.Bytes.Present(value, Context()).Text);
        }

        [Theory]
        [InlineData(0.38, "380 ms")]
        [InlineData(4.2, "4.2 s")]
        [InlineData(65, "1 m 05 s")]
        [InlineData(8040, "2 h 14 m")]
        [InlineData(273600, "3 d 4 h")]
        public void Duration_Seconds_PickTheReadableUnit(double seconds, string expected) {
            Assert.Equal(expected, CellPresenters.Duration.Present(TimeSpan.FromSeconds(seconds), Context()).Text);
        }

        [Fact]
        public void Date_Displays_AbsoluteRelativeAndBothInTheTimeZone() {
            DateTimeOffset value = Now.AddHours(-3);
            Assert.Equal("2026-10-16 09:00", new DatePresenter().Present(value, Context()).Text);
            Assert.Equal("3 h ago", new DatePresenter(DateDisplay.Relative).Present(value, Context()).Text);
            Assert.Equal("2026-10-16 09:00 (3 h ago)", new DatePresenter(DateDisplay.Both).Present(value, Context()).Text);
            Assert.Equal("in 12 d", new DatePresenter(DateDisplay.Relative).Present(Now.AddDays(12), Context()).Text);
            TimeZoneInfo warsaw = TimeZoneInfo.CreateCustomTimeZone("plus2", TimeSpan.FromHours(2), "plus2", "plus2");
            Assert.Equal("2026-10-16 11:00", new DatePresenter(timeZone: warsaw).Present(value, Context()).Text);
        }

        [Fact]
        public void Status_AsciiAndFull_SymbolAndWord() {
            Assert.Equal("✗ FAIL", CellPresenters.Status.Present("failed", Context()).Text);
            Assert.Equal("[FAIL]", CellPresenters.Status.Present(StatusKind.Fail, Context(GlyphTier.Ascii)).Text);
            Assert.Equal("custom", CellPresenters.Status.Present("custom", Context()).Text);
        }

        [Fact]
        public void Boolean_Tiers_SymbolPlusWord() {
            Assert.Equal("✓ yes", CellPresenters.Boolean.Present(true, Context()).Text);
            Assert.Equal("no", CellPresenters.Boolean.Present(false, Context(GlyphTier.Ascii)).Text);
            Assert.Equal(ThemeToken.Warn, new BooleanPresenter(trueIsProblem: true).Present(true, Context()).Tone);
        }

        [Fact]
        public void ToneRule_WithoutColour_AddsTheStatusSymbol() {
            ToneRule[] rules = { ToneRule.Above(100, ThemeToken.Warn) };
            PresentedValue colour = CellPresenters.Present(133, new NumberPresenter("ms"), rules, Context());
            PresentedValue plain = CellPresenters.Present(133, new NumberPresenter("ms"), rules, Context(GlyphTier.Ascii, ColorDepth.None));
            Assert.Equal("133 ms", colour.Text);
            Assert.Equal(ThemeToken.Warn, colour.Tone);
            Assert.Equal("[WARN] 133 ms", plain.Text);
            Assert.Equal("12 ms", CellPresenters.Present(12, new NumberPresenter("ms"), rules, Context(GlyphTier.Ascii, ColorDepth.None)).Text);
        }

        [Fact]
        public void Link_Terminals_Osc8InWindowsTerminalTextElsewhere() {
            LinkPresenter presenter = new LinkPresenter(text: v => "Docs", address: v => "https://evotec.xyz/docs");
            PresentedValue linked = presenter.Present("x", Context(kind: TerminalKind.WindowsTerminal));
            PresentedValue plain = presenter.Present("x", Context(kind: TerminalKind.Conhost));
            Assert.Equal("Docs", linked.Text);
            Assert.Equal("https://evotec.xyz/docs", linked.Line[0].Style.Link);
            Assert.Equal("Docs (https://evotec.xyz/docs)", plain.Text);
        }

        [Fact]
        public void Visuals_BarSparklineProgress_CarryTheirData() {
            PresentedValue bar = CellPresenters.DataBar.Present(50, Context().WithColumnMaximum(200));
            Assert.Equal(CellVisualKind.Bar, bar.Visual);
            Assert.Equal(0.25, bar.Fraction, 3);
            PresentedValue spark = CellPresenters.Sparkline.Present(new[] { 1.0, 3, 2, 5 }, Context());
            Assert.Equal(CellVisualKind.Sparkline, spark.Visual);
            Assert.Equal("5", spark.Text);
            PresentedValue progress = CellPresenters.Progress.Present("18/30", Context());
            Assert.Equal(0.6, progress.Fraction, 3);
            Assert.Equal("18/30", progress.Text);
        }

        [Fact]
        public void FromName_KnownAndUnknownNames() {
            Assert.IsType<BytesPresenter>(CellPresenters.FromName("Bytes"));
            Assert.Equal("5 km", ((NumberPresenter)CellPresenters.FromName("number", "km")).Present(5, Context()).Text);
            Assert.Throws<ArgumentException>(() => CellPresenters.FromName("Nope"));
        }
    }
}
