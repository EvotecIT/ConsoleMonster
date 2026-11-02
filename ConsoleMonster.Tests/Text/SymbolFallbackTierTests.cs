using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class SymbolFallbackTierTests {
        public static IEnumerable<object[]> EmojiCatalog() {
            string[] symbols = { "💚", "💙", "💔", "🧪", "ℹ️", "😠", "⁉️", "❓", "❔", "💤", "⚠️", "⬆️",
                "🆙", "🚦", "👎", "✅", "🌡️", "🛑", "💀", "📌", "🧡", "⛔", "📛", "📝", "🔒", "💡",
                "•", "△", "▽", "🌶️", "📧", "✉️", "🔚", "🤯", "🕵️", "❌", "📋", "🔄", "⛓", "⛓️‍💥", "☹" };
            foreach (GlyphTier tier in new[] { GlyphTier.Basic, GlyphTier.Ascii }) {
                foreach (string symbol in symbols) yield return new object[] { tier, symbol };
            }
        }

        [Theory]
        [MemberData(nameof(EmojiCatalog))]
        public void Apply_CatalogSymbol_HasNoEmojiOrNonAsciiFallback(GlyphTier tier, string symbol) {
            string actual = SymbolFallback.Apply(symbol + " Result", tier);
            Assert.DoesNotContain(actual, char.IsSurrogate);
            Assert.DoesNotContain('\uFE0F', actual);
            Assert.DoesNotContain('\u200D', actual);
            if (tier == GlyphTier.Ascii) Assert.All(actual, value => Assert.InRange((int)value, 0x20, 0x7E));
            if (tier == GlyphTier.Basic) Assert.All(actual, value =>
                Assert.True(value <= '~' || "√×•§↑↓→←■·□".Contains(value)));
        }

        [Theory]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Apply_UnknownEmoji_ReplacesTheWholeCluster(GlyphTier tier) {
            Assert.Equal("* Result", SymbolFallback.Apply("🫎Result", tier));
            Assert.Equal("* Result", SymbolFallback.Apply("👩🏽‍🚀Result", tier));
        }

        [Fact]
        public void Apply_JoinedEmoji_UsesOneWidthPreservingFallback() {
            Assert.Equal("~ Result", SymbolFallback.Apply("⛓️‍💥Result", GlyphTier.Basic));
            Assert.Equal("~ Result", SymbolFallback.Apply("⛓️‍💥Result", GlyphTier.Ascii));
        }

        [Theory]
        [InlineData("📋")]
        [InlineData("⚠️")]
        [InlineData("✅")]
        [InlineData("✔️")]
        [InlineData("❌")]
        [InlineData("ℹ️")]
        public void Apply_NarrowFallbackBeforeText_PreservesSourceCells(string symbol) {
            string actual = SymbolFallback.Apply(symbol + "Result", GlyphTier.Basic);
            Assert.Equal(DisplayWidth.Measure(symbol + "Result"), DisplayWidth.Measure(actual));
            Assert.Contains(" Result", actual);
            Assert.DoesNotContain("  Result", SymbolFallback.Apply(symbol + " Result", GlyphTier.Basic));
        }

        [Fact]
        public void Apply_StyledBoundary_KeepsTheSeparator() {
            Assert.Equal("√ Result", SymbolFallback.Apply(Markup.Parse("✅[green]Result[/]"), GlyphTier.Basic).Lines[0].PlainText);
            Assert.Equal("√ Result", SymbolFallback.Apply(Markup.Parse("[green]✅[/] Result"), GlyphTier.Basic).Lines[0].PlainText);
            Assert.Equal("~ Result", SymbolFallback.Apply(Markup.Parse("⛓️[green]‍💥Result[/]"), GlyphTier.Basic).Lines[0].PlainText);
        }

        [Fact]
        public void Apply_AsciiChecks_UseTheSameSuccessGlyphAndReadableBadge() {
            Assert.Equal("+ selected", SymbolFallback.Apply("✓ selected", GlyphTier.Ascii));
            Assert.Equal("[OK] done", SymbolFallback.Apply("✅ done", GlyphTier.Ascii));
            Assert.Equal("[OK] done", SymbolFallback.Apply("✔️ done", GlyphTier.Ascii));
        }

        [Theory]
        [InlineData("△", "↑", "^")]
        [InlineData("▽", "↓", "v")]
        [InlineData("☐", "□", "[ ]")]
        [InlineData("🚦", "•", "*")]
        [InlineData("🛑", "■", "#")]
        public void Apply_StatusSymbolsRetainTheirMeaning(string symbol, string basic, string ascii) {
            var basicProfile = TestTerminal.Profile(glyphs: GlyphTier.Basic);
            var asciiProfile = TestTerminal.Profile(glyphs: GlyphTier.Ascii);
            Assert.Equal(basic, SymbolFallback.Apply(symbol, basicProfile.Glyphs, basicProfile.WidthProfile).TrimEnd());
            Assert.Equal(ascii, SymbolFallback.Apply(symbol, asciiProfile.Glyphs, asciiProfile.WidthProfile).TrimEnd());
        }

        [Theory]
        [InlineData(GlyphTier.Basic, "√")]
        [InlineData(GlyphTier.Ascii, "[OK]")]
        public void Apply_SentencePunctuation_StaysAttachedToTheFallback(GlyphTier tier, string expected) {
            foreach (string suffix in new[] { ".", ", ready", ": ready", ")" }) {
                Assert.Equal(expected + suffix, SymbolFallback.Apply("✅" + suffix, tier));
                StyledText styled = SymbolFallback.Apply(Markup.Parse("[green]✅[/]" + suffix), tier);
                Assert.Equal(expected + suffix, styled.Lines[0].PlainText);
            }
        }

        [Theory]
        [InlineData("💡", "*")]
        [InlineData("🕵️", "?")]
        public void Apply_HintAndSearch_AreDistinctFromWarning(string symbol, string expected) {
            Assert.Equal(expected + " Result", SymbolFallback.Apply(symbol + " Result", GlyphTier.Basic));
            Assert.Equal(expected + " Result", SymbolFallback.Apply(symbol + " Result", GlyphTier.Ascii));
        }

        [Theory]
        [MemberData(nameof(EmojiCatalog))]
        public void GetGlyphAndPrefix_Catalog_KeepOneCellAndAnAlignedSeparator(GlyphTier tier, string symbol) {
            string glyph = SymbolFallback.GetGlyph(symbol, tier);
            string prefix = SymbolFallback.GetPrefix(symbol, tier);
            Assert.Equal(1, DisplayWidth.Measure(glyph));
            Assert.Equal(glyph + " ", prefix);
            Assert.Equal(2, DisplayWidth.Measure(prefix));
            if (tier == GlyphTier.Ascii) Assert.All(prefix, value => Assert.InRange((int)value, 0x20, 0x7E));
        }

        [Fact]
        public void GetGlyph_BadgesAndFace_NeverReturnsATruncatedToken() {
            Assert.Equal("#", SymbolFallback.GetGlyph("📝", GlyphTier.Ascii));
            Assert.Equal("-", SymbolFallback.GetGlyph("👎", GlyphTier.Ascii));
            Assert.Equal("+", SymbolFallback.GetGlyph("✅", GlyphTier.Ascii));
            Assert.Equal("+", SymbolFallback.GetGlyph("✓", GlyphTier.Ascii));
            Assert.Equal(GlyphSet.Ascii.Get(Glyph.Check), SymbolFallback.GetGlyph("✓", GlyphTier.Ascii));
            Assert.Equal("[OK]", GlyphSet.Ascii.StatusMarker(StatusKind.Pass));
            Assert.Equal("✅ ", SymbolFallback.GetPrefix("✅", GlyphTier.Full));
        }

        [Theory]
        [InlineData("✓", "+")]
        [InlineData("✅", "+")]
        [InlineData("☑", "+")]
        public void GetGlyph_BasicAmbiguousWidthTwo_UsesTheAsciiCellMeaning(string symbol, string expected) {
            var profile = new TerminalWidthProfile(ambiguousWidth: 2);
            Assert.Equal(expected, SymbolFallback.GetGlyph(symbol, GlyphTier.Basic, profile));
            Assert.Equal(expected + " ", SymbolFallback.GetPrefix(symbol, GlyphTier.Basic, profile));
            Assert.Equal(1, DisplayWidth.Measure(SymbolFallback.GetGlyph(symbol, GlyphTier.Basic, profile), profile));
        }

        [Fact]
        public void GetGlyph_UnicodeEmojiCatalog_AllReducedTiersStaySingleCell() {
            foreach (int[] ranges in new[] { UnicodeWidthTables.EmojiPresentation, UnicodeWidthTables.ExtendedPictographic }) {
                for (int index = 0; index < ranges.Length; index += 2) {
                    for (int scalar = ranges[index]; scalar <= ranges[index + 1]; scalar++) {
                        string symbol = char.ConvertFromUtf32(scalar);
                        foreach (GlyphTier tier in new[] { GlyphTier.Basic, GlyphTier.Ascii }) {
                            string glyph = SymbolFallback.GetGlyph(symbol, tier);
                            Assert.Equal(1, DisplayWidth.Measure(glyph));
                            Assert.DoesNotContain(glyph, char.IsSurrogate);
                            if (tier == GlyphTier.Ascii) Assert.All(glyph, value => Assert.InRange((int)value, 0x20, 0x7E));
                        }
                    }
                }
            }
        }
    }
}
