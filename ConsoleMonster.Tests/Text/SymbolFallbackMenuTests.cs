using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class SymbolFallbackMenuTests {
        private static readonly HashSet<int> Wgl4 = LoadWgl4();
        public static IEnumerable<object[]> MenuSymbols() {
            int[] scalars = { 0x2610, 0x2611, 0x2612, 0x2699, 0x23ED, 0x23F9, 0x23FB };
            string[] basic = { "·", "√", "×", "*", "»", "■", "○" };
            string[] ascii = { "-", "+", "x", "*", ">", "#", "O" };
            for (int i = 0; i < scalars.Length; i++) {
                foreach (bool selector in new[] { false, true }) {
                    yield return new object[] { scalars[i], selector, GlyphTier.Basic, basic[i] };
                    yield return new object[] { scalars[i], selector, GlyphTier.Ascii, ascii[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(MenuSymbols))]
        public void MenuSymbols_ReducedTier_UseCompleteAlignedSemanticGlyphs(int scalar, bool selector, GlyphTier tier, string expected) {
            string symbol = char.ConvertFromUtf32(scalar) + (selector ? "\uFE0F" : string.Empty);
            Assert.Equal(expected, SymbolFallback.GetGlyph(symbol, tier));
            Assert.Equal(expected + " ", SymbolFallback.GetPrefix(symbol, tier));
            Assert.Equal(expected, SymbolFallback.Apply(symbol, tier).TrimEnd());
            Assert.Equal(expected + " Label", SymbolFallback.Apply(symbol + "Label", tier));
            Assert.Equal(expected + " Label", SymbolFallback.Apply(symbol + " Label", tier));
            Assert.Equal(2, DisplayWidth.Measure(SymbolFallback.GetPrefix(symbol, tier)));
            Assert.Equal(symbol, SymbolFallback.Apply(symbol, GlyphTier.Full));
            Assert.Equal(symbol, SymbolFallback.GetGlyph(symbol, GlyphTier.Full));
        }

        [Theory]
        [InlineData(0x2100, 0x214F)] // Letterlike symbols; trademark is ordinary prose
        [InlineData(0x2150, 0x218F)] // Number forms
        [InlineData(0x2190, 0x21FF)] // Arrows
        [InlineData(0x2200, 0x22FF)] // Mathematical operators
        [InlineData(0x2300, 0x23FF)] // Miscellaneous technical
        [InlineData(0x2400, 0x243F)] // Control pictures
        [InlineData(0x2440, 0x245F)] // Optical character recognition
        [InlineData(0x2460, 0x24FF)] // Enclosed alphanumerics
        [InlineData(0x2500, 0x257F)] // Box drawing
        [InlineData(0x2580, 0x259F)] // Block elements
        [InlineData(0x25A0, 0x25FF)] // Geometric shapes
        [InlineData(0x2600, 0x26FF)] // Miscellaneous symbols
        [InlineData(0x2700, 0x27BF)] // Dingbats
        [InlineData(0x27C0, 0x27EF)] // Miscellaneous mathematical symbols A
        [InlineData(0x27F0, 0x27FF)] // Supplemental arrows A
        [InlineData(0x2800, 0x28FF)] // Braille patterns
        [InlineData(0x2900, 0x297F)] // Supplemental arrows B
        [InlineData(0x2980, 0x29FF)] // Miscellaneous mathematical symbols B
        [InlineData(0x2A00, 0x2AFF)] // Supplemental mathematical operators
        [InlineData(0x2B00, 0x2BFF)] // Miscellaneous symbols and arrows
        [InlineData(0x1F000, 0x1FAFF)] // Supplementary symbols and pictographs
        public void UnicodeSymbolBlocks_ReducedTiers_NeverLeakUnsupportedGlyphs(int start, int end) {
            for (int scalar = start; scalar <= end; scalar++) {
                foreach (GlyphTier tier in new[] { GlyphTier.Basic, GlyphTier.Ascii }) {
                    foreach (string selector in new[] { string.Empty, "\uFE0F", "\uFE0E" }) {
                        string symbol = char.ConvertFromUtf32(scalar) + selector;
                        string text = SymbolFallback.Apply(symbol, tier);
                        if (scalar == 0x2122 && selector != "\uFE0F") {
                            // Trademark notices remain ordinary prose. Cell APIs are still tier-restricted.
                            Assert.Equal(symbol, text);
                        } else {
                            AssertSafe(text, tier, scalar);
                        }
                        string glyph = SymbolFallback.GetGlyph(symbol, tier);
                        AssertSafe(glyph, tier, scalar);
                        Assert.Equal(1, DisplayWidth.Measure(glyph));
                        Assert.Equal(2, DisplayWidth.Measure(SymbolFallback.GetPrefix(symbol, tier)));
                    }
                }
            }
        }

        [Fact]
        public void BasicRepertoire_CompressedRanges_MatchMicrosoftWgl4Fixture() {
            for (int scalar = 0; scalar <= 0xFFFF; scalar++) {
                Assert.Equal(Wgl4.Contains(scalar), Wgl4Repertoire.Contains(scalar));
            }
        }

        [Fact]
        public void RegisteredFallback_UnsupportedReplacement_UsesLastResortGlyph() {
            SymbolFallback.Register("\uE000", "☐", "⚙");
            foreach (GlyphTier tier in new[] { GlyphTier.Basic, GlyphTier.Ascii }) {
                Assert.Equal("*", SymbolFallback.Apply("\uE000", tier));
                Assert.Equal("*", SymbolFallback.GetGlyph("\uE000", tier));
            }
        }

        [Fact]
        public void Apply_ExistingBasicStatusMarkers_UseAsciiStatusRatherThanLastResort() {
            Assert.Equal("+ PASS", SymbolFallback.Apply("√ PASS", GlyphTier.Ascii));
            Assert.Equal("√ PASS", SymbolFallback.Apply("√ PASS", GlyphTier.Basic));
            Assert.Equal("x FAIL", SymbolFallback.Apply("× FAIL", GlyphTier.Ascii));
            Assert.Equal("× FAIL", SymbolFallback.Apply("× FAIL", GlyphTier.Basic));
            Assert.Equal("*", SymbolFallback.Apply("±", GlyphTier.Ascii));
            Assert.Equal("±", SymbolFallback.Apply("±", GlyphTier.Basic));
        }

        private static void AssertSafe(string value, GlyphTier tier, int source) {
            Assert.All(value, glyph => Assert.True(glyph >= ' ' && glyph <= '~'
                || tier == GlyphTier.Basic && Wgl4.Contains(glyph),
                "Unsupported fallback for U+" + source.ToString("X") + ": U+" + ((int)glyph).ToString("X")));
        }

        private static HashSet<int> LoadWgl4() {
            HashSet<int> scalars = new HashSet<int>();
            using (Stream stream = typeof(SymbolFallbackMenuTests).Assembly.GetManifestResourceStream("ConsoleMonster.Tests.Text.Fixtures.Wgl4.codepoints.txt")
                ?? throw new InvalidOperationException("Missing WGL4 fixture.")) {
                using (StreamReader reader = new StreamReader(stream)) {
                    string? line;
                    while ((line = reader.ReadLine()) != null) {
                        if (line.Length > 0 && line[0] != '#') scalars.Add(int.Parse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    }
                }
            }
            return scalars;
        }
    }
}
