using System.Globalization;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// Golden snapshots of the table showcases (DNS records, mail security checks, a run summary and build artifacts) per glyph tier at
    /// full width (120), about 80 (hidden columns with a footer) and 50 cells (record cards). The profile fixes the culture to
    /// en-US while the thread culture is pl-PL, so the goldens prove the explicit culture wins; one pl-PL golden proves the
    /// other way round.
    /// </summary>
    public sealed class TableShowcaseGoldenTests {
        [Theory]
        [InlineData(GlyphTier.Full, 120)]
        [InlineData(GlyphTier.Full, 80)]
        [InlineData(GlyphTier.Full, 50)]
        [InlineData(GlyphTier.Basic, 120)]
        [InlineData(GlyphTier.Basic, 80)]
        [InlineData(GlyphTier.Basic, 50)]
        [InlineData(GlyphTier.Ascii, 120)]
        [InlineData(GlyphTier.Ascii, 80)]
        [InlineData(GlyphTier.Ascii, 50)]
        public void TableShowcases_TierAndWidth_MatchGolden(GlyphTier tier, int width) {
            string text = WithThreadCulture("pl-PL", () => Terminal.RenderText(TableShowcases.All(), TerminalProfile.Fixed(width, 60, ColorDepth.None, tier).WithCulture(EnUs), Theme.Dark));
            Golden.Match("tables." + tier.ToString().ToLowerInvariant() + "." + width.ToString(CultureInfo.InvariantCulture) + ".txt", text);
        }

        [Fact]
        public void TableShowcases_Redirected_PlainTextWithoutEscapes() {
            TerminalProfile profile = TerminalProfile.Fixed(100, 60, ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain).WithCulture(EnUs);
            string text = WithThreadCulture("pl-PL", () => Terminal.RenderText(TableShowcases.All(), profile, Theme.Dark));
            Assert.True(text.IndexOf((char)27) < 0, "redirected output must not contain escape sequences");
            Assert.Contains("[FAIL]", text);
            Golden.Match("tables.plain.100.txt", text);
        }

        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

        [Fact]
        public void TableShowcases_PolishCulture_UsesPolishSeparators() {
            TerminalProfile profile = TerminalProfile.Fixed(120, 60, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.GetCultureInfo("pl-PL"));
            string text = WithThreadCulture("en-US", () => Terminal.RenderText(TableShowcases.Sections("summary,artifacts"), profile, Theme.Dark));
            Assert.Contains("71,5 %", text);
            Assert.Contains("1,41 MB", text);
            Golden.Match("tables.pl-pl.120.txt", text);
        }

        [Fact]
        public void TableShowcases_EnglishCulture_UsesEnglishSeparators() {
            string text = WithThreadCulture("pl-PL", () => Terminal.RenderText(TableShowcases.Sections("summary,artifacts"), TerminalProfile.Fixed(120, 60, ColorDepth.None, GlyphTier.Full).WithCulture(EnUs), Theme.Dark));
            Assert.Contains("71.5 %", text);
            Assert.Contains("1.41 MB", text);
            Assert.Contains("Showing 5 of 1,234", text);
        }

        internal static string WithThreadCulture(string name, System.Func<string> render) {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(name);
            try {
                return render();
            } finally {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        internal static string WithInvariantCulture(System.Func<string> render) {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try {
                return render();
            } finally {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
