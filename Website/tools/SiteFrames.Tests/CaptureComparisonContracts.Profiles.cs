using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    public sealed partial class CaptureComparisonContracts {
        // Same public comparison boundary and Run/Change/Compare fixture helpers as the existing contracts.
        [Theory]
        [InlineData("profile")]
        [InlineData("profile-ci")]
        [InlineData("profile-detected")]
        public void NativeProfileValuesAreReportedWithoutDroppingCellDiagnostics(string name) {
            string a = ProfileRun("a", "macos", name, ProfileText(name, false));
            string b = ProfileRun("b", "windows", name, ProfileText(name, true));
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            JsonNode entry = Assert.Single(report["allowed"]!.AsArray())!;
            Assert.True(entry["changedRowCount"]!.GetValue<int>() > 0);
            Assert.True(entry["changedCellCount"]!.GetValue<long>() > 0);
            Assert.NotEmpty(entry["changedRows"]!.AsArray());
            Assert.NotEmpty(entry["changedRows"]![0]!["cells"]!.AsArray());
            var changes = entry["valueDifferences"]!.AsArray().Select(v => v!["key"]!.GetValue<string>()).ToArray();
            Assert.Equal(name == "profile-detected" ? new[] { "ColorDepth", "Glyphs", "FontFace", "colour", "glyphs", "kind" } : new[] { "FontFace", "kind" }, changes);
            Assert.Empty(report["failures"]!.AsArray());
        }

        [Theory]
        [InlineData("FontFace         Consolas", "FontFace         Cascadia Mono", "FontFace")]
        [InlineData("  kind: Conhost", "  kind: Xterm", "kind")]
        [InlineData("ColorDepth       TrueColor", "ColorDepth       Ansi256", "ColorDepth")]
        [InlineData("Glyphs           Basic", "Glyphs           Full", "Glyphs")]
        [InlineData("  colour: TrueColor (Windows build >=14931)", "  colour: TrueColor (native reason)", "colour")]
        [InlineData("  glyphs: Basic (conhost font 'Consolas')", "  glyphs: Basic (native reason)", "glyphs")]
        public void EachDetectedValueIsIndividuallyReported(string before, string after, string key) {
            string original = ProfileText("profile-detected", true);
            string a = ProfileRun("a", "windows", "profile-detected", original);
            string b = ProfileRun("b", "macos", "profile-detected", original.Replace(before, after, StringComparison.Ordinal));
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            JsonNode value = Assert.Single(Assert.Single(report["allowed"]!.AsArray())!["valueDifferences"]!.AsArray())!;
            Assert.Equal(key, value["key"]!.GetValue<string>());
        }

        [Theory]
        [InlineData("Mode             Interactive", "Mode             Stream")]
        [InlineData("Width            100", "Width            99")]
        [InlineData("Height           40", "Height           39")]
        [InlineData("ThemePreference  Auto", "ThemePreference  Light")]
        [InlineData("  mode: Interactive (VT terminal)", "  mode: Stream (CI environment)")]
        [InlineData("  size: 100x40", "  size: 99x40")]
        [InlineData("  culture: en-US", "  culture: pl-PL")]
        [InlineData("  CONSOLEMONSTER_EMOJI_WIDTH: 2", "  CONSOLEMONSTER_EMOJI_WIDTH: 1")]
        [InlineData("  CONSOLEMONSTER_AMBIGUOUS_WIDTH: 1", "  CONSOLEMONSTER_AMBIGUOUS_WIDTH: 2")]
        [InlineData("  width profile: emoji=2", "  width profile: emoji=1")]
        [InlineData("  probe widths: False", "  probe widths: True")]
        [InlineData("  VT: supported", "  VT: unavailable")]
        [InlineData("  theme preference: Auto", "  theme preference: Dark")]
        [InlineData("never redirected or CI)", "never redirected or XX)")]
        [InlineData("redirected or CI)", "redirected\r\nor CI)")]
        [InlineData("This console", "Other console")]
        [InlineData("  kind: Conhost", "  kind: Conhost\r\n  unexpected: value")]
        [InlineData("  kind: Conhost", "  kind: Conhost\r\n  kind: Xterm")]
        [InlineData("FontFace         Consolas", "FontFace         Consolas\r\nFontFace         Other")]
        [InlineData("  culture: en-US (CONSOLEMONSTER_CULTURE)\r\n", "")]
        public void StableFieldsMissingDuplicateAndUnknownRowsFailClosed(string before, string after) {
            string a = ProfileRun("a", "macos", "profile-detected", ProfileText("profile-detected", false));
            string b = ProfileRun("b", "windows", "profile-detected", ProfileText("profile-detected", true).Replace(before, after, StringComparison.Ordinal));
            AssertProfileFailure(a, b);
        }

        [Theory]
        [InlineData("profile", "ColorDepth       TrueColor", "ColorDepth       Ansi256")]
        [InlineData("profile", "Glyphs           Full", "Glyphs           Basic")]
        [InlineData("profile", "  colour: TrueColor (CONSOLEMONSTER_COLOR)", "  colour: TrueColor (native reason)")]
        [InlineData("profile", "  glyphs: Full (CONSOLEMONSTER_GLYPHS)", "  glyphs: Full (native reason)")]
        [InlineData("profile-ci", "ColorDepth       – None", "ColorDepth       TrueColor")]
        [InlineData("profile-ci", "Glyphs           Basic", "Glyphs           Full")]
        [InlineData("profile-ci", "  colour: None (NO_COLOR)", "  colour: TrueColor (native reason)")]
        [InlineData("profile-ci", "  glyphs: Basic (stream/CI)", "  glyphs: Full (native reason)")]
        public void PinnedAndCiColourAndGlyphFieldsAndDecisionsStayStrict(string name, string before, string after) {
            string a = ProfileRun("a", "macos", name, ProfileText(name, false));
            string b = ProfileRun("b", "windows", name, ProfileText(name, true).Replace(before, after, StringComparison.Ordinal));
            AssertProfileFailure(a, b);
        }

        [Theory]
        [InlineData("source")]
        [InlineData("hash")]
        [InlineData("width")]
        [InlineData("height")]
        [InlineData("screen")]
        [InlineData("complete")]
        [InlineData("unrecorded-source")]
        public void ProfileValuesCannotOverrideFreshnessGeometryDefaultsOrCompletion(string field) {
            const string file = "docs/profile.windows.ansi";
            string a = ProfileRun("a", "macos", "profile", ProfileText("profile", false));
            string b = ProfileRun("b", "windows", "profile", ProfileText("profile", true));
            if (field == "source") { File.WriteAllText(Path.Combine(b, "sources.json"), new JsonObject { [file] = "1111111111111111" }.ToJsonString()); }
            else if (field == "unrecorded-source") { File.Delete(Path.Combine(b, "sources.json")); }
            else if (field == "hash") { File.AppendAllText(Path.Combine(b, file), "changed"); }
            else { Change(b, m => {
                if (field == "complete") { m[field] = false; }
                else { m["files"]![file]![field] = field == "screen" ? JsonValue.Create("dark") : JsonValue.Create(field == "width" ? 99 : 39); }
            }); }
            AssertProfileFailure(a, b);
        }

        [Theory]
        [InlineData("\u001b[31m")]
        [InlineData("\u001b[41m")]
        [InlineData("\u001b[4m")]
        [InlineData("\u001b]8;;https://example.test\u001b\\")]
        public void ProfileValuePolicyDoesNotHidePaintOrLinkChanges(string style) {
            string a = ProfileRun("a", "macos", "profile-detected", ProfileText("profile-detected", false));
            string changed = ProfileText("profile-detected", true).Replace("  kind: Conhost", style + "  kind: Conhost\u001b[0m\u001b]8;;\u001b\\", StringComparison.Ordinal);
            AssertProfileFailure(a, ProfileRun("b", "windows", "profile-detected", changed));
        }

        [Fact]
        public void NativeLabelQuantizationIsAllowedOnlyForDetectedProfiles() {
            string a = ProfileRun("a", "macos", "profile-detected", ProfileText("profile-detected", false, paintLabels: true));
            string b = ProfileRun("b", "windows", "profile-detected", ProfileText("profile-detected", true, paintLabels: true));
            Assert.Equal(0, Compare(a, b, out _));
            a = ProfileRun("c", "macos", "profile", ProfileText("profile", false, paintLabels: true));
            string changed = ProfileText("profile", true, paintLabels: true).Replace("38;2;139;143;154", "38;5;245", StringComparison.Ordinal);
            AssertProfileFailure(a, ProfileRun("d", "windows", "profile", changed));
        }

        [Fact]
        public void AllCompleteFramesAndTheirCountsMustRemainComparable() {
            string Prefix(string s) => "\u001b[?1049h\u001b[?2026h" + s + "\u001b[?2026l\u001b[?1049l";
            string mac = ProfileText("profile", false), win = ProfileText("profile", true);
            string macFinal = mac.Replace("en-US", "pl-PL", StringComparison.Ordinal), winFinal = win.Replace("en-US", "pl-PL", StringComparison.Ordinal);
            string a = ProfileRun("a", "macos", "profile", Prefix(mac) + macFinal);
            string b = ProfileRun("b", "windows", "profile", Prefix(win) + winFinal);
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            Assert.Equal(new[] { 0, 0, 1, 1 }, Assert.Single(report["allowed"]!.AsArray())!["valueDifferences"]!.AsArray().Select(v => v!["frame"]!.GetValue<int>()).ToArray());
            // Final frame is valid; the preceding committed frame is changed or missing.
            b = ProfileRun("c", "windows", "profile", Prefix(win.Replace("Dark", "Light", StringComparison.Ordinal)) + winFinal);
            AssertProfileFailure(a, b);
            AssertProfileFailure(a, ProfileRun("d", "windows", "profile", winFinal));
        }

        [Fact]
        public void QualityIsCheckedBeforeNativeValueAllowance() {
            string a = ProfileRun("a", "macos", "profile", ProfileText("profile", false).Replace("  kind: Xterm", "  kind: \uFFFD", StringComparison.Ordinal));
            string b = ProfileRun("b", "windows", "profile", ProfileText("profile", true));
            AssertProfileFailure(a, b);
        }

        [Fact]
        public void SamePlatformBytesAndExplicitNativeTagsRemainRequired() {
            string a = ProfileRun("a", "windows", "profile", ProfileText("profile", false));
            string b = ProfileRun("b", "windows", "profile", ProfileText("profile", true));
            AssertProfileFailure(a, b);
            Assert.Equal(0, CaptureComparison.Run(b, b));
            a = ProfileRun("c", "macos", "profile", ProfileText("profile", false), tagged: false);
            AssertProfileFailure(a, b);
        }

        private void AssertProfileFailure(string a, string b) {
            Assert.Equal(1, Compare(a, b, out JsonObject report));
            Assert.Empty(report["allowed"]!.AsArray());
            Assert.NotEmpty(report["failures"]!.AsArray());
        }

        private string ProfileRun(string id, string platform, string name, string text, bool tagged = true) {
            string file = "docs/" + name + (tagged ? "." + platform : "") + ".ansi";
            string root = Run(id, platform, text, file);
            Change(root, m => { m["files"]![file]!["width"] = 100; m["files"]![file]!["height"] = 40; });
            return root;
        }

        private static string ProfileText(string name, bool windows, bool paintLabels = false) {
            bool detected = name == "profile-detected", ci = name == "profile-ci";
            string depth = ci ? "– None" : detected && !windows ? "Ansi256" : "TrueColor";
            string glyphs = ci || detected && windows ? "Basic" : "Full";
            string mode = ci ? "Stream" : "Interactive", theme = detected ? "Auto" : "Dark";
            string Field(string key, string value) {
                string label = key.PadRight(17);
                if (paintLabels && !ci) { label = (detected && !windows ? "\u001b[38;5;245m" : "\u001b[38;2;139;143;154m") + label + "\u001b[0m"; }
                return label + value;
            }
            string colour = ci ? "None (NO_COLOR)" : detected ? windows ? "TrueColor (Windows build >=14931)" : "Ansi256 (TERM 256color)" : "TrueColor (CONSOLEMONSTER_COLOR)";
            string glyphReason = ci ? "Basic (stream/CI)" : detected ? windows ? "Basic (conhost font 'Consolas')" : "Full (modern terminal, locale LC_ALL=en_US.UTF-8)" : "Full (CONSOLEMONSTER_GLYPHS)";
            return string.Join("\r\n", new[] {
                "This console", Field("Mode", mode), Field("ColorDepth", depth), Field("Glyphs", glyphs), Field("Width", "100"),
                Field("Height", "40"), Field("ThemePreference", theme)
            }.Concat(windows ? new[] { Field("FontFace", "Consolas") } : Array.Empty<string>()).Concat(new[] {
                "", "  CONSOLEMONSTER_EMOJI_WIDTH: 2", "  CONSOLEMONSTER_AMBIGUOUS_WIDTH: 1",
                "  hyperlinks: False (terminal/CONSOLEMONSTER_HYPERLINKS; requires interactive VT, never redirected or CI)",
                "  mode: " + mode + (ci ? " (CI environment)" : " (VT terminal)"), "  colour: " + colour, "  glyphs: " + glyphReason,
                "  VT: supported", "  kind: " + (windows ? "Conhost" : "Xterm"),
                "  width profile: emoji=2, ambiguous=1, zwj=Joined, flag=2, text=1 (environment)", "  probe widths: False",
                "  theme preference: " + theme + " (CONSOLEMONSTER_THEME)", "  culture: en-US (CONSOLEMONSTER_CULTURE)",
                "  size: 100x40 (visible window or defaults)", ""
            }));
        }
    }
}
