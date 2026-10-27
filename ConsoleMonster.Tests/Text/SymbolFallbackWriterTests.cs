using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Output;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    [Collection("Live sessions")]
    public sealed class SymbolFallbackWriterTests {
        [Theory]
        [InlineData(GlyphTier.Basic, "~ Result\n√ Result\n")]
        [InlineData(GlyphTier.Ascii, "~ Result\n[OK] Result\n")]
        public void DirectWriters_ReducedTier_ReplacesBeforeEmission(GlyphTier tier, string expected) {
            TerminalProfile profile = new TerminalProfile(ColorDepth.None, tier, OutputMode.Stream);
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                Terminal.WriteMarkupLine("⛓️‍💥Result", profile, writer);
                Terminal.WriteRich(RichText.Plain("✅Result"), profile: profile, writer: writer);
                Assert.Equal(expected, writer.ToString().Replace("\r\n", "\n"));
            }
        }

        [Theory]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Markup_OneCellEmojiProfile_KeepsAnInlineSeparator(GlyphTier tier) {
            TerminalProfile profile = new TerminalProfile(ColorDepth.None, tier, OutputMode.Stream,
                widthProfile: new TerminalWidthProfile(emojiWidth: 1));
            Assert.Equal("i Result", SymbolFallback.Apply("ℹ️Result", tier, profile.WidthProfile));
            Assert.Equal("~ Result", SymbolFallback.Apply("🔄Result", tier, profile.WidthProfile));
            Assert.Equal("~ Result", SymbolFallback.Apply("🔄 Result", tier, profile.WidthProfile));
            Assert.Equal("i Result", Terminal.CreateContext(profile).Markup("ℹ️Result").Lines[0].PlainText);
            Assert.Equal("~ Result", Terminal.CreateContext(profile).Markup("🔄Result").Lines[0].PlainText);
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                Terminal.WriteMarkupLine("ℹ️Result", profile, writer);
                Terminal.WriteRich(RichText.Plain("ℹ️Result"), profile: profile, writer: writer);
                Assert.Equal("i Result\ni Result\n", writer.ToString().Replace("\r\n", "\n"));
            }
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
