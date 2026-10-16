using System.Globalization;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// Golden snapshots of the table showcases (DnsClientX, DomainDetective, TestimoX, PSPublishModule) per glyph tier at
    /// full width (120), about 80 (hidden columns with a footer) and 50 cells (record cards), with invariant culture.
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
            string text = WithInvariantCulture(() => Terminal.RenderText(TableShowcases.All(), TerminalProfile.Fixed(width, 60, ColorDepth.None, tier), Theme.Dark));
            Golden.Match("tables." + tier.ToString().ToLowerInvariant() + "." + width.ToString(CultureInfo.InvariantCulture) + ".txt", text);
        }

        [Fact]
        public void TableShowcases_Redirected_PlainTextWithoutEscapes() {
            TerminalProfile profile = TerminalProfile.Fixed(100, 60, ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain);
            string text = WithInvariantCulture(() => Terminal.RenderText(TableShowcases.All(), profile, Theme.Dark));
            Assert.True(text.IndexOf((char)27) < 0, "redirected output must not contain escape sequences");
            Assert.Contains("[FAIL]", text);
            Golden.Match("tables.plain.100.txt", text);
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
