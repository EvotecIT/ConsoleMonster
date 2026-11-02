using System.Globalization;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Presenters;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>Emoji and symbol fallback per glyph tier, and :shortcode: emoji.</summary>
    public sealed class SymbolFallbackTests {
        [Fact]
        public void Apply_MapsSymbolsPerTier() {
            string[] symbols = { "✅", "✔️", "✓", "❌", "⚠️", "➜", "→", "ℹ️", "🚀", "⏳", "🟢", "…", "—", "“q”" };
            StringBuilder table = new StringBuilder();
            foreach (string symbol in symbols) {
                table.Append(symbol).Append(" | ").Append(SymbolFallback.Apply(symbol, GlyphTier.Basic)).Append(" | ").Append(SymbolFallback.Apply(symbol, GlyphTier.Ascii)).Append('\n');
            }
            Golden.Match("symbols.fallback.txt", table.ToString());
            Assert.Equal("✅ done", SymbolFallback.Apply("✅ done", GlyphTier.Full));
        }

        [Fact]
        public void Apply_LeavesPlainTextAloneAndDropsStraySelectors() {
            string plain = "plain ASCII and Łódź";
            Assert.Same(plain, SymbolFallback.Apply(plain, GlyphTier.Ascii));
            Assert.Equal("x", SymbolFallback.Apply("x️", GlyphTier.Ascii));
        }

        [Fact]
        public void Register_AddsAnAppSymbol() {
            SymbolFallback.Register("🦄", "U", "[unicorn]");
            Assert.True(SymbolFallback.TryGet("🦄", GlyphTier.Ascii, out string ascii));
            Assert.Equal("[unicorn]", ascii);
            Assert.Equal("U ok", SymbolFallback.Apply("🦄 ok", GlyphTier.Basic));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Table_WithEmoji_KeepsCellTextAndWidthsPerTier(GlyphTier tier) {
            Table table = new Table().AddColumn("Check").AddColumn("Result");
            table.AddValues("✅ backup", "ok");
            table.AddValues("⚠️ disk", "80% full");
            table.AddRow(new TableCell("[warn]❌ sync[/]"), "failed");
            string text = TestTerminal.RenderText(table, 40, glyphs: tier);
            Golden.Match("symbols.table." + tier.ToString().ToLowerInvariant() + ".txt", text);
            string[] lines = text.Split('\n');
            Assert.All(lines, line => Assert.Equal(DisplayWidth.Measure(lines[0]), DisplayWidth.Measure(line)));
        }

        [Fact]
        public void Markup_Shortcodes_AreOptIn() {
            Assert.Equal(":rocket: go", Markup.Strip(":rocket: go"));
            try {
                Markup.Shortcodes = true;
                Assert.Equal("🚀 go :nope: 10:30", Markup.Parse(":rocket: go :nope: 10:30").Lines[0].PlainText);
            } finally {
                Markup.Shortcodes = false;
            }
        }

        [Fact]
        public void EmojiPresenter_ReplacesShortcodesInColumnValues() {
            Table table = new Table().AddColumn(new TableColumn("Step") { Presenter = CellPresenters.Emoji });
            table.AddValues(":white_check_mark: deploy");
            Assert.Contains("[OK] deploy", TestTerminal.RenderText(table, 30, glyphs: GlyphTier.Ascii));
            Assert.Contains("✅ deploy", TestTerminal.RenderText(table, 30));
        }
    }
}
