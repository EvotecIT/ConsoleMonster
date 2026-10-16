using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    public sealed class CapabilityDetectorTests {
        [Theory]
        [InlineData("light", ThemePreference.Light, false)]
        [InlineData("DARK", ThemePreference.Dark, false)]
        [InlineData("auto", ThemePreference.Auto, false)]
        [InlineData("", ThemePreference.Auto, false)]
        [InlineData("sepia", ThemePreference.Auto, true)]
        public void Detect_ThemeEnvironmentValue_UsesPreferenceOrRecordsIgnoredValue(string value, ThemePreference expected, bool invalid) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["TERM"] = "xterm", ["CONSOLEMONSTER_THEME"] = value
            })).Detect();
            Assert.Equal(expected, profile.ThemePreference);
            Assert.Equal(invalid, profile.Decisions.Any(decision => decision.Contains("CONSOLEMONSTER_THEME: ignored invalid value")));
        }

        [Fact]
        public void Fixed_DefaultsThemePreferenceToAutoAndCopyCanOverrideIt() {
            TerminalProfile profile = TerminalProfile.Fixed(80, 24);
            Assert.Equal(ThemePreference.Auto, profile.ThemePreference);
            Assert.Equal(ThemePreference.Dark, profile.WithThemePreference(ThemePreference.Dark).ThemePreference);
        }

        [Fact]
        public void RefreshFont_UsesNewFontAndPreservesGlyphOverrideAndAmbiguousWidthDowngrade() {
            CapabilityDetector detector = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["CONSOLEMONSTER_GLYPHS"] = "full"
            }, isWindows: true));
            TerminalProfile profile = TerminalProfile.Fixed(80, 24).WithWidthProfile(TerminalWidthProfile.Standard.WithAmbiguousWidth(2));
            TerminalProfile refreshed = detector.RefreshFont(profile, "Cascadia Mono");
            Assert.Equal("Cascadia Mono", refreshed.FontFace);
            Assert.Equal(GlyphTier.Ascii, refreshed.Glyphs);
            Assert.Contains(refreshed.Decisions, decision => decision.Contains("CONSOLEMONSTER_GLYPHS"));
            Assert.Contains(refreshed.Decisions, decision => decision.Contains("ambiguous width 2"));
        }

        [Theory]
        [InlineData(22631, "Consolas", false, ColorDepth.TrueColor, GlyphTier.Basic)]
        [InlineData(14393, "Consolas", false, ColorDepth.Ansi16, GlyphTier.Basic)]
        [InlineData(10586, "Consolas", false, ColorDepth.Ansi16, GlyphTier.Basic)]
        [InlineData(14930, "Lucida Console", false, ColorDepth.Ansi16, GlyphTier.Basic)]
        [InlineData(14931, "Courier New", false, ColorDepth.TrueColor, GlyphTier.Basic)]
        [InlineData(22631, "Cascadia Code", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Cascadia Mono", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "JetBrains Mono", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Fira Code", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "DejaVu Sans Mono", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Hack", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Iosevka", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Source Code Pro", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Ubuntu Mono", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "MesloLGS NF", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Example Nerd Font", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "Example NF", false, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(22631, "UnknownFont", false, ColorDepth.TrueColor, GlyphTier.Basic)]
        [InlineData(22631, "Terminal", false, ColorDepth.TrueColor, GlyphTier.Ascii)]
        [InlineData(22631, "Other", true, ColorDepth.TrueColor, GlyphTier.Ascii)]
        public void ConhostUsesBuildAndFont(int build, string font, bool raster, ColorDepth color, GlyphTier glyphs) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(isWindows: true,
                windowsBuild: build, isConsoleHandle: true, virtualTerminalEnabled: true,
                consoleFontFace: font, consoleFontIsRaster: raster, windowWidth: 94, windowHeight: 27)).Detect();
            Assert.Equal(OutputMode.Interactive, profile.Mode);
            Assert.Equal(TerminalKind.Conhost, profile.Kind);
            Assert.Equal(color, profile.ColorDepth);
            Assert.Equal(glyphs, profile.Glyphs);
            Assert.True(profile.IsLive);
            Assert.True(profile.IsConsoleHandle);
            Assert.Equal(94, profile.Width);
            Assert.Equal(27, profile.Height);
            Assert.Contains(profile.Decisions, decision => decision.Contains(font));
        }

        [Theory]
        [InlineData("WT_SESSION", "session", TerminalKind.WindowsTerminal)]
        [InlineData("TERM_PROGRAM", "vscode", TerminalKind.VisualStudioCode)]
        public void ModernWindowsTerminalsUseTrueColorAndFullGlyphs(string name, string value, TerminalKind kind) {
            TerminalProfile profile = Windows(new Dictionary<string, string> { [name] = value });
            Assert.Equal(kind, profile.Kind);
            Assert.Equal(ColorDepth.TrueColor, profile.ColorDepth);
            Assert.Equal(GlyphTier.Full, profile.Glyphs);
            Assert.True(profile.IsLive);
        }

        [Theory]
        [InlineData("xterm-256color", "", ColorDepth.Ansi256)]
        [InlineData("xterm", "", ColorDepth.Ansi16)]
        [InlineData("screen", "", ColorDepth.Ansi16)]
        [InlineData("xterm-256color", "truecolor", ColorDepth.TrueColor)]
        [InlineData("xterm", "24bit", ColorDepth.TrueColor)]
        [InlineData("unknown", "", ColorDepth.None)]
        public void UnixTtyUsesTerminalHints(string term, string colorTerm, ColorDepth expected) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["TERM"] = term, ["COLORTERM"] = colorTerm
            }, windowWidth: 80, windowHeight: 24)).Detect();
            Assert.Equal(TerminalKind.Xterm, profile.Kind);
            Assert.Equal(OutputMode.Interactive, profile.Mode);
            Assert.Equal(expected, profile.ColorDepth);
            Assert.Equal(GlyphTier.Full, profile.Glyphs);
            Assert.True(profile.IsLive);
        }

        [Theory]
        [InlineData("CI")]
        [InlineData("GITHUB_ACTIONS")]
        [InlineData("TF_BUILD")]
        [InlineData("BUILD_BUILDID")]
        [InlineData("JENKINS_URL")]
        [InlineData("GITLAB_CI")]
        [InlineData("TEAMCITY_VERSION")]
        public void CiLogsUseStreamAnsi16AndBasic(string variable) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                [variable] = "1", ["WT_SESSION"] = "inherited", ["TERM"] = "xterm-256color"
            }, isWindows: true, isOutputRedirected: true)).Detect();
            Assert.True(profile.IsContinuousIntegration);
            Assert.Equal(OutputMode.Stream, profile.Mode);
            Assert.Equal(ColorDepth.Ansi16, profile.ColorDepth);
            Assert.Equal(GlyphTier.Basic, profile.Glyphs);
            Assert.False(profile.IsLive);
        }

        [Fact]
        public void RedirectedOutputUsesEnvironmentSizeAndNoColor() {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["COLUMNS"] = "75", ["LINES"] = "19", ["TERM"] = "xterm-256color", ["WT_SESSION"] = "inherited"
            }, isOutputRedirected: true, windowWidth: 500, windowHeight: 500)).Detect();
            Assert.Equal(TerminalKind.Redirected, profile.Kind);
            Assert.Equal(OutputMode.Stream, profile.Mode);
            Assert.Equal(ColorDepth.None, profile.ColorDepth);
            Assert.Equal(GlyphTier.Basic, profile.Glyphs);
            Assert.Equal(75, profile.Width);
            Assert.Equal(19, profile.Height);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("0", "-1")]
        [InlineData("invalid", "99999999999999")]
        public void InvalidRedirectedDimensionsUseDefaults(string columns, string lines) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["COLUMNS"] = columns, ["LINES"] = lines
            }, isOutputRedirected: true)).Detect();
            Assert.Equal(TerminalProfile.DefaultWidth, profile.Width);
            Assert.Equal(TerminalProfile.DefaultHeight, profile.Height);
        }

        [Theory]
        [InlineData("interactive", OutputMode.Interactive)]
        [InlineData("live", OutputMode.Interactive)]
        [InlineData("stream", OutputMode.Stream)]
        [InlineData("plain", OutputMode.Plain)]
        public void ModeOverrideWinsAutomaticSelection(string value, OutputMode expected) {
            TerminalProfile profile = Windows(new Dictionary<string, string> {
                ["CONSOLEMONSTER_MODE"] = value, ["CI"] = "true"
            });
            Assert.Equal(expected, profile.Mode);
        }

        [Fact]
        public void ExplicitPlainModeForcesNoColorAndAsciiDespiteOtherOverrides() {
            TerminalProfile profile = Windows(new Dictionary<string, string> {
                ["CONSOLEMONSTER_MODE"] = "plain", ["CONSOLEMONSTER_COLOR"] = "truecolor",
                ["CONSOLEMONSTER_GLYPHS"] = "full", ["WT_SESSION"] = "session"
            });
            Assert.Equal(OutputMode.Plain, profile.Mode);
            Assert.Equal(ColorDepth.None, profile.ColorDepth);
            Assert.Equal(GlyphTier.Ascii, profile.Glyphs);
            Assert.False(profile.IsLive);
        }

        [Theory]
        [InlineData("16", ColorDepth.Ansi16)]
        [InlineData("256", ColorDepth.Ansi256)]
        [InlineData("truecolor", ColorDepth.TrueColor)]
        public void ExplicitColorOverrideAppliesToRedirectedOutput(string value, ColorDepth expected) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["CONSOLEMONSTER_COLOR"] = value
            }, isOutputRedirected: true)).Detect();
            Assert.Equal(OutputMode.Stream, profile.Mode);
            Assert.Equal(expected, profile.ColorDepth);
        }

        [Theory]
        [InlineData("none", ColorDepth.None)]
        [InlineData("16", ColorDepth.Ansi16)]
        [InlineData("256", ColorDepth.Ansi256)]
        [InlineData("truecolor", ColorDepth.TrueColor)]
        [InlineData("24bit", ColorDepth.TrueColor)]
        [InlineData("auto", ColorDepth.None)]
        [InlineData("invalid", ColorDepth.None)]
        public void ColorOverrideBeatsNoColorExceptAuto(string value, ColorDepth expected) {
            TerminalProfile profile = Windows(new Dictionary<string, string> {
                ["CONSOLEMONSTER_COLOR"] = value, ["NO_COLOR"] = "1"
            });
            Assert.Equal(expected, profile.ColorDepth);
        }

        [Theory]
        [InlineData("full", GlyphTier.Full)]
        [InlineData("basic", GlyphTier.Basic)]
        [InlineData("ascii", GlyphTier.Ascii)]
        public void GlyphOverrideWinsFontSelection(string value, GlyphTier expected) {
            Assert.Equal(expected, Windows(new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = value }).Glyphs);
        }

        [Theory]
        [InlineData("1", ColorDepth.None)]
        [InlineData(" ", ColorDepth.None)]
        [InlineData("", ColorDepth.TrueColor)]
        public void NoColorUsesNonEmptyPresence(string value, ColorDepth expected) {
            Assert.Equal(expected, Windows(new Dictionary<string, string> { ["NO_COLOR"] = value }).ColorDepth);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DumbTerminalIsPlainIncludingCi(bool ci) {
            TerminalProfile profile = Windows(new Dictionary<string, string> {
                ["TERM"] = "dumb", ["CI"] = ci ? "true" : "false",
                ["CONSOLEMONSTER_COLOR"] = "truecolor", ["CONSOLEMONSTER_GLYPHS"] = "full"
            });
            Assert.Equal(OutputMode.Plain, profile.Mode);
            Assert.Equal(ColorDepth.None, profile.ColorDepth);
            Assert.Equal(GlyphTier.Ascii, profile.Glyphs);
            Assert.False(profile.IsLive);
        }

        [Theory]
        [InlineData(7601, false)]
        [InlineData(22631, false)]
        public void VtEnableFailureFallsBackToStream(int build, bool vt) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(isWindows: true,
                windowsBuild: build, isConsoleHandle: true, virtualTerminalEnabled: vt)).Detect();
            Assert.Equal(OutputMode.Stream, profile.Mode);
            Assert.False(profile.SupportsVirtualTerminal);
            Assert.Equal(ColorDepth.None, profile.ColorDepth);
        }

        [Theory]
        [InlineData(false, TerminalKind.Unknown)]
        [InlineData(true, TerminalKind.PowerShellIse)]
        public void WindowsWithoutConsoleUsesStream(bool ise, TerminalKind kind) {
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["PSISE"] = ise ? "1" : "", ["TERM"] = "xterm"
            }, isWindows: true)).Detect();
            Assert.Equal(kind, profile.Kind);
            Assert.Equal(OutputMode.Stream, profile.Mode);
            Assert.False(profile.IsConsoleHandle);
        }

        [Fact]
        public void SnapshotCopiesVariablesAndUsesPlatformCaseRules() {
            Dictionary<string, string> variables = new Dictionary<string, string> { ["consolemonster_color"] = "16" };
            TerminalEnvironment windows = new TerminalEnvironment(variables, isWindows: true,
                isConsoleHandle: true, virtualTerminalEnabled: true, windowsBuild: 22631);
            TerminalEnvironment unix = new TerminalEnvironment(variables);
            variables["consolemonster_color"] = "none";
            CapabilityDetector detector = new CapabilityDetector(windows);
            Assert.Equal(ColorDepth.Ansi16, detector.Detect().ColorDepth);
            Assert.False(unix.EnvironmentVariables.ContainsKey("CONSOLEMONSTER_COLOR"));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)windows.EnvironmentVariables).Add("X", "1"));
        }

        private static TerminalProfile Windows(Dictionary<string, string> variables) {
            return new CapabilityDetector(new TerminalEnvironment(variables, isWindows: true,
                windowsBuild: 22631, isConsoleHandle: true, virtualTerminalEnabled: true,
                consoleFontFace: "Consolas")).Detect();
        }

        [Fact]
        public void WidthOverrides_ApplyAndDowngradeAmbiguousGlyphs() {
            TerminalProfile profile = Windows(new Dictionary<string, string> {
                ["CONSOLEMONSTER_EMOJI_WIDTH"] = "1", ["CONSOLEMONSTER_AMBIGUOUS_WIDTH"] = "2"
            });
            Assert.Equal(1, profile.WidthProfile.EmojiWidth);
            Assert.Equal(2, profile.WidthProfile.AmbiguousWidth);
            Assert.Equal(GlyphTier.Ascii, profile.Glyphs);
            Assert.False(profile.ProbeWidths);
            Assert.Contains(profile.Decisions, value => value.Contains("ambiguous width 2"));
        }

        [Fact]
        public void WidthOverrides_InvalidValueIsIgnoredAndNoted() {
            TerminalProfile profile = Windows(new Dictionary<string, string> { ["CONSOLEMONSTER_EMOJI_WIDTH"] = "3", ["CONSOLEMONSTER_AMBIGUOUS_WIDTH"] = "1" });
            Assert.Equal(2, profile.WidthProfile.EmojiWidth);
            Assert.False(profile.ProbeWidths);
            Assert.Contains(profile.Decisions, value => value.Contains("ignored invalid value"));
        }

        [Fact]
        public void WidthOverrides_AutoEnablesProbeOnlyForInteractiveUnredirectedInput() {
            TerminalProfile interactive = Windows(new Dictionary<string, string>());
            TerminalProfile redirected = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["TERM"] = "xterm", ["CONSOLEMONSTER_EMOJI_WIDTH"] = "auto"
            }, isOutputRedirected: true)).Detect();
            Assert.True(interactive.ProbeWidths);
            Assert.False(redirected.ProbeWidths);
        }
    }
}
