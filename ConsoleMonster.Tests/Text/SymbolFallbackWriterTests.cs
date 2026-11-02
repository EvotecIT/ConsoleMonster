using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class SymbolFallbackWriterTests {
        [Theory]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Apply_OneCellEmojiProfile_KeepsAnInlineSeparator(GlyphTier tier) {
            TerminalWidthProfile widths = new TerminalWidthProfile(emojiWidth: 1);
            Assert.Equal("i Result", SymbolFallback.Apply("ℹ️Result", tier, widths));
            Assert.Equal("~ Result", SymbolFallback.Apply("🔄Result", tier, widths));
            Assert.Equal("~ Result", SymbolFallback.Apply("🔄 Result", tier, widths));
        }

        [Theory]
        [InlineData("Acme™", "Acme™")]
        [InlineData("© 2026", "© 2026")]
        [InlineData("© 2026 Łódź", "© 2026 Łódź")]
        [InlineData("Acme® ™︎ text", "Acme® ™︎ text")]
        [InlineData("☀︎ text", "* text")]
        public void Apply_TextPresentation_PreservesProseAndDowngradesUnsupportedDecorations(string text, string expected) {
            Assert.Equal(expected, SymbolFallback.Apply(text, GlyphTier.Basic));
            Assert.Equal(expected, SymbolFallback.Apply(text, GlyphTier.Ascii));
        }

        [Fact]
        public void Apply_UnchangedCombiningCluster_PreservesItsSpanStyles() {
            StyledText original = Markup.Parse("e[red]́[/]✅", depth: ColorDepth.TrueColor);
            StyledText actual = SymbolFallback.Apply(original, GlyphTier.Basic);
            TextSpan accent = Assert.Single(actual.Lines[0], span => span.Text.Contains("́"));
            Assert.Equal(original.Lines[0][1].Style, accent.Style);
            Assert.Equal("́", accent.Text);
            Assert.Equal("é√ ", actual.Lines[0].PlainText);
        }
    }
}
