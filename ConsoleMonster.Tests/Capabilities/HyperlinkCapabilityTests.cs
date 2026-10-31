using System.Collections.Generic;
using System.Globalization;
using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    public sealed class HyperlinkCapabilityTests {
        [Theory]
        [InlineData("WT_SESSION", "session", TerminalKind.WindowsTerminal, true)]
        [InlineData("TERM_PROGRAM", "vscode", TerminalKind.VisualStudioCode, true)]
        [InlineData("TERM_PROGRAM", "iTerm.app", TerminalKind.ITerm2, true)]
        [InlineData("TERM_PROGRAM", "WezTerm", TerminalKind.WezTerm, true)]
        [InlineData("VTE_VERSION", "4999", TerminalKind.Vte, false)]
        [InlineData("VTE_VERSION", "5000", TerminalKind.Vte, true)]
        [InlineData("VTE_VERSION", "7802", TerminalKind.Vte, true)]
        [InlineData("VTE_VERSION", "invalid", TerminalKind.Vte, false)]
        [InlineData("KONSOLE_VERSION", "220399", TerminalKind.Konsole, false)]
        [InlineData("KONSOLE_VERSION", "220400", TerminalKind.Konsole, true)]
        [InlineData("KONSOLE_VERSION", "240801", TerminalKind.Konsole, true)]
        [InlineData("KONSOLE_VERSION", "invalid", TerminalKind.Konsole, false)]
        [InlineData("TERM_PROGRAM", "Apple_Terminal", TerminalKind.Xterm, false)]
        public void Detect_HostAndVersion_ChooseSupport(string name, string value, TerminalKind kind, bool supported) {
            TerminalProfile profile = Detect(new Dictionary<string, string> { [name] = value });
            Assert.Equal(kind, profile.Kind);
            Assert.Equal(supported, profile.SupportsHyperlinks);
        }

        [Theory]
        [InlineData("on", true)]
        [InlineData("TRUE", true)]
        [InlineData("1", true)]
        [InlineData("off", false)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        [InlineData("auto", false)]
        [InlineData("invalid", false)]
        public void Detect_Override_CanEnableUnknownTerminal(string value, bool expected) {
            TerminalProfile profile = Detect(new Dictionary<string, string> { ["CONSOLEMONSTER_HYPERLINKS"] = value });
            Assert.Equal(expected, profile.SupportsHyperlinks);
            if (value == "invalid") {
                Assert.Contains(profile.Decisions, note => note.Contains("ignored invalid value"));
            }
            TerminalProfile known = Detect(new Dictionary<string, string> { ["WT_SESSION"] = "session", ["CONSOLEMONSTER_HYPERLINKS"] = "off" });
            Assert.False(known.SupportsHyperlinks);
        }

        [Theory]
        [InlineData("CI")]
        [InlineData("GITHUB_ACTIONS")]
        [InlineData("TF_BUILD")]
        [InlineData("BUILD_BUILDID")]
        [InlineData("JENKINS_URL")]
        [InlineData("GITLAB_CI")]
        [InlineData("TEAMCITY_VERSION")]
        public void Detect_CiCannotBeOverridden(string ci) {
            TerminalProfile profile = Detect(new Dictionary<string, string> {
                [ci] = "1", ["WT_SESSION"] = "session", ["CONSOLEMONSTER_HYPERLINKS"] = "on", ["CONSOLEMONSTER_MODE"] = "interactive"
            });
            Assert.True(profile.IsContinuousIntegration);
            Assert.False(profile.SupportsHyperlinks);
            Assert.False(profile.WithHyperlinks(true).SupportsHyperlinks);
        }

        [Fact]
        public void Detect_RedirectionCannotBeOverriddenEvenWithVtHandle() {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["WT_SESSION"] = "session", ["CONSOLEMONSTER_HYPERLINKS"] = "on", ["CONSOLEMONSTER_MODE"] = "interactive"
            }, isOutputRedirected: true, isWindows: true, isConsoleHandle: true, virtualTerminalEnabled: true)).Detect();
            Assert.True(profile.SupportsVirtualTerminal);
            Assert.False(profile.SupportsHyperlinks);
            Assert.False(profile.WithHyperlinks(true).SupportsHyperlinks);
        }

        [Theory]
        [InlineData(OutputMode.Stream, true)]
        [InlineData(OutputMode.Plain, true)]
        [InlineData(OutputMode.Interactive, false)]
        public void Profile_RequiresInteractiveVt(OutputMode mode, bool vt) {
            TerminalProfile profile = new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full, mode,
                supportsVirtualTerminal: vt, supportsHyperlinks: true);
            Assert.False(profile.SupportsHyperlinks);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CopiesAndFontRefresh_PreserveSupportAndLanguage(bool support) {
            TerminalEnvironment environment = new TerminalEnvironment(new Dictionary<string, string> { ["TERM"] = "xterm" });
            TerminalProfile profile = new CapabilityDetector(environment).Detect().WithHyperlinks(support).WithLanguage(CultureInfo.GetCultureInfo("pl-PL"))
                .WithImageProtocol(ImageProtocol.Kitty);
            TerminalProfile[] copies = {
                profile.WithSize(70, 20), profile.WithColorDepth(ColorDepth.None), profile.WithGlyphs(GlyphTier.Basic),
                profile.WithDecision("test"), profile.WithCulture(CultureInfo.InvariantCulture), profile.WithLanguage(CultureInfo.GetCultureInfo("pl-PL")),
                profile.WithThemePreference(ThemePreference.Light), profile.WithWidthProfile(TerminalWidthProfile.Standard),
                profile.WithMode(OutputMode.Stream).WithMode(OutputMode.Interactive), new CapabilityDetector(environment).RefreshFont(profile, "Cascadia Mono")
            };
            foreach (TerminalProfile copy in copies) {
                Assert.Equal(support, copy.SupportsHyperlinks);
                Assert.Equal("pl-PL", copy.Language?.Name);
                Assert.Equal(ImageProtocol.Kitty, copy.ImageSupport);
            }
        }

        [Fact]
        public void ExistingConstructorSignature_RemainsAvailableToCompiledConsumers() {
            Type[] signature = {
                typeof(ColorDepth), typeof(GlyphTier), typeof(OutputMode), typeof(int), typeof(int), typeof(TerminalKind),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(IReadOnlyList<string>),
                typeof(TerminalWidthProfile), typeof(bool), typeof(ThemePreference), typeof(CultureInfo)
            };
            System.Reflection.ConstructorInfo? constructor = typeof(TerminalProfile).GetConstructor(signature);
            Assert.NotNull(constructor);
            TerminalProfile profile = Assert.IsType<TerminalProfile>(constructor!.Invoke(new object?[] {
                ColorDepth.TrueColor, GlyphTier.Full, OutputMode.Interactive, 80, 24, TerminalKind.WindowsTerminal,
                true, false, false, false, null, null, null, false, ThemePreference.Auto, null
            }));
            Assert.True(profile.SupportsHyperlinks);
            Assert.False(profile.WithMode(OutputMode.Stream).SupportsHyperlinks);
        }

        private static TerminalProfile Detect(Dictionary<string, string> variables) {
            variables["TERM"] = "xterm-256color";
            return new CapabilityDetector(new TerminalEnvironment(variables)).Detect();
        }
    }
}
