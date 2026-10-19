using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    /// <summary>On Unix the glyph tier follows the character-type locale (LC_ALL, then LC_CTYPE, then LANG).</summary>
    public sealed class LocaleGlyphTests {
        private static TerminalProfile Unix(Dictionary<string, string> variables) {
            variables["TERM"] = "xterm-256color";
            return new CapabilityDetector(new TerminalEnvironment(variables, windowWidth: 80, windowHeight: 24)).Detect();
        }

        [Theory]
        [InlineData("C.UTF-8", GlyphTier.Full)]
        [InlineData("en_US.UTF-8", GlyphTier.Full)]
        [InlineData("pl_PL.utf8", GlyphTier.Full)]
        [InlineData("de_DE.UTF-8@euro", GlyphTier.Full)]
        [InlineData("UTF-8", GlyphTier.Full)]
        [InlineData("utf8", GlyphTier.Full)]
        [InlineData("C", GlyphTier.Ascii)]
        [InlineData("POSIX", GlyphTier.Ascii)]
        [InlineData("en_US", GlyphTier.Ascii)]
        [InlineData("en_US.ISO-8859-1", GlyphTier.Ascii)]
        public void Lang_DecidesTheTier(string lang, GlyphTier expected) {
            TerminalProfile profile = Unix(new Dictionary<string, string> { ["LANG"] = lang });
            Assert.Equal(expected, profile.Glyphs);
            Assert.Contains(profile.Decisions, decision => decision.StartsWith("glyphs: " + expected, System.StringComparison.Ordinal) && decision.Contains("LANG=" + lang));
        }

        [Fact]
        public void LcAll_WinsOverLcCtypeAndLang() {
            Assert.Equal(GlyphTier.Ascii, Unix(new Dictionary<string, string> { ["LC_ALL"] = "C", ["LC_CTYPE"] = "en_US.UTF-8", ["LANG"] = "en_US.UTF-8" }).Glyphs);
            Assert.Equal(GlyphTier.Full, Unix(new Dictionary<string, string> { ["LC_ALL"] = "en_US.UTF-8", ["LANG"] = "C" }).Glyphs);
        }

        [Fact]
        public void LcCtype_WinsOverLangAndEmptyValuesAreSkipped() {
            Assert.Equal(GlyphTier.Ascii, Unix(new Dictionary<string, string> { ["LC_CTYPE"] = "POSIX", ["LANG"] = "en_US.UTF-8" }).Glyphs);
            Assert.Equal(GlyphTier.Full, Unix(new Dictionary<string, string> { ["LC_ALL"] = "", ["LC_CTYPE"] = "C.UTF-8", ["LANG"] = "C" }).Glyphs);
        }

        [Fact]
        public void NoLocale_KeepsFullGlyphs() {
            TerminalProfile profile = Unix(new Dictionary<string, string>());
            Assert.Equal(GlyphTier.Full, profile.Glyphs);
            Assert.Contains(profile.Decisions, decision => decision.Contains("locale not set"));
        }

        [Fact]
        public void ExplicitOverride_StillWinsOverTheLocale() {
            TerminalProfile profile = Unix(new Dictionary<string, string> { ["LANG"] = "C", ["CONSOLEMONSTER_GLYPHS"] = "full" });
            Assert.Equal(GlyphTier.Full, profile.Glyphs);
            Assert.Contains(profile.Decisions, decision => decision == "glyphs: Full (CONSOLEMONSTER_GLYPHS)");
        }

        [Fact]
        public void RefreshFont_AppliesTheSameLocaleRule() {
            Dictionary<string, string> variables = new Dictionary<string, string> { ["TERM"] = "xterm-256color", ["LC_ALL"] = "C" };
            CapabilityDetector detector = new CapabilityDetector(new TerminalEnvironment(variables, windowWidth: 80, windowHeight: 24));
            TerminalProfile refreshed = detector.RefreshFont(detector.Detect(), "DejaVu Sans Mono");
            Assert.Equal(GlyphTier.Ascii, refreshed.Glyphs);
            Assert.Contains(refreshed.Decisions, decision => decision.Contains("LC_ALL=C"));
        }

        [Fact]
        public void Windows_IgnoresTheLocale() {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> { ["LANG"] = "C", ["WT_SESSION"] = "1" },
                isWindows: true, windowsBuild: 22631, isConsoleHandle: true, virtualTerminalEnabled: true)).Detect();
            Assert.Equal(GlyphTier.Full, profile.Glyphs);
        }

        [Fact]
        public void Redirected_StaysBasicWhateverTheLocale() {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> { ["TERM"] = "xterm", ["LANG"] = "en_US.UTF-8" }, isOutputRedirected: true)).Detect();
            Assert.Equal(GlyphTier.Basic, profile.Glyphs);
            Assert.Single(profile.Decisions, d => d.StartsWith("glyphs:", System.StringComparison.Ordinal));
        }
    }
}
