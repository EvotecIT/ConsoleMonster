using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class CodePageTextTests {
        [Fact]
        public void RedirectedOutputOnALegacyCodePageKeepsTextThatCodePageCanEncode() {
            string text = Capture(Redirected(852));

            // The parent decodes code page 852, which holds these letters but not the box-drawing rule glyph.
            Assert.Contains("Zażółć", text);
            Assert.Contains("--", text);
            Assert.DoesNotContain("─", text);
        }

        [Fact]
        public void ExplicitAsciiStillTransliteratesText() {
            string text = Capture(Redirected(852, new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = "ascii" }));

            Assert.Contains("Zazolc", text);
            Assert.DoesNotContain("Zażółć", text);
        }

        [Fact]
        public void StreamedLongTableKeepsTextThatCodePageCanEncode() {
            TerminalProfile profile = Redirected(852);
            Table table = new Table().Column("Name");
            for (int i = 0; i <= Terminal.StreamingRowThreshold; i++) table.Row("Zażółć");
            StringTerminalWriter writer = new StringTerminalWriter();

            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                foreach (CellBuffer block in Terminal.RenderChunks(table, profile.Width, Terminal.CreateContext(profile))) output.WriteBlock(block);
            }

            string text = writer.ToString();
            Assert.Contains("Zażółć", text);
            Assert.DoesNotContain("Zazolc", text);
        }

        [Theory]
        [InlineData(852, 0x017C, true)]      // ż
        [InlineData(54936, 0x20000, true)]   // a supplementary ideograph GB18030 encodes as a surrogate pair
        [InlineData(852, 0x20000, false)]
        public void AsciiTextKeepsWholeCodePointsTheCodePageCanEncode(int codePage, int codePoint, bool kept) {
            string glyph = char.ConvertFromUtf32(codePoint);

            using (SymbolFallback.KeepCodePageText(Redirected(codePage))) {
                string constrained = SymbolFallback.Constrain(glyph, GlyphTier.Ascii);

                Assert.Equal(kept, constrained == glyph);
            }
        }

        [Fact]
        public void AsciiTextDropsAnIsolatedSurrogate() {
            using (SymbolFallback.KeepCodePageText(Redirected(54936))) {
                Assert.Equal("a?", SymbolFallback.Constrain("a\uD840", GlyphTier.Ascii));
            }
        }

        private static TerminalProfile Redirected(int codePage, Dictionary<string, string>? variables = null) {
#if NET
            // Legacy code pages need the provider on .NET; PowerShell and most console hosts register it.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            return new CapabilityDetector(new TerminalEnvironment(variables ?? new Dictionary<string, string>(), isWindows: true,
                isOutputRedirected: true, isConsoleHandle: false).WithOutputCodePage(codePage)).Detect();
        }

        private static string Capture(TerminalProfile profile) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                output.WriteBlock(Terminal.Render(new Rule("Zażółć"), profile));
            }
            return writer.ToString();
        }
    }
}
