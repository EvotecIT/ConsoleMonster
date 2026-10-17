using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// Golden frames per glyph tier and VT byte streams per colour depth. They protect the visual contract: a change in any
    /// widget's cells or in the escape sequences sent to the terminal shows up as a reviewable golden-file diff.
    /// </summary>
    public sealed class GoldenFrameTests {
        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ReportFrameMatchesGoldenPerTier(GlyphTier tier) {
            TerminalProfile profile = TerminalProfile.Fixed(100, 40, ColorDepth.TrueColor, tier);
            Golden.Match("report." + tier.ToString().ToLowerInvariant() + ".txt", Terminal.RenderText(TestScenes.Report(), profile, Theme.Dark));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void LiveWidgetsFrameMatchesGoldenPerTier(GlyphTier tier) {
            TerminalProfile profile = TerminalProfile.Fixed(100, 40, ColorDepth.TrueColor, tier);
            Golden.Match("live." + tier.ToString().ToLowerInvariant() + ".txt", Terminal.RenderText(TestScenes.Live(), profile, Theme.Dark));
        }

        [Theory]
        [InlineData(ColorDepth.TrueColor)]
        [InlineData(ColorDepth.Ansi256)]
        [InlineData(ColorDepth.Ansi16)]
        [InlineData(ColorDepth.None)]
        public void VtByteStreamMatchesGoldenPerColorDepth(ColorDepth depth) {
            TerminalProfile profile = TerminalProfile.Fixed(60, 20, depth, GlyphTier.Full);
            string encoded = Terminal.RenderEncoded(TestScenes.Report(), profile, Theme.Dark);
            Golden.Match("report.vt." + depth.ToString().ToLowerInvariant() + ".txt", encoded.Replace("\u001b", "\\e"));
        }

        [Fact]
        public void AsciiTierRendersOnlyPrintableAscii() {
            TerminalProfile profile = TerminalProfile.Fixed(100, 40, ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain);
            string text = Terminal.RenderText(TestScenes.Report(), profile) + Terminal.RenderText(TestScenes.Live(), profile);
            char[] offenders = text.Where(c => c != '\n' && (c < 32 || c > 126)).Distinct().ToArray();
            Assert.True(offenders.Length == 0, "Non-ASCII characters in ASCII tier: " + new string(offenders));
        }

        [Fact]
        public void PlainModeNeverEmitsEscapeSequences() {
            TerminalProfile profile = TerminalProfile.Fixed(100, 40, ColorDepth.TrueColor, GlyphTier.Ascii, OutputMode.Plain);
            string encoded = Terminal.RenderEncoded(TestScenes.Live(), profile);
            int index = encoded.IndexOf('\u001b');
            Assert.True(index < 0, "Escape found near: " + (index < 0 ? string.Empty : encoded.Substring(System.Math.Max(0, index - 30), System.Math.Min(60, encoded.Length - System.Math.Max(0, index - 30))).Replace("\u001b", "^")));
        }
    }
}
