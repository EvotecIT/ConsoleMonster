using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Capabilities {
    /// <summary>Resolves terminal capabilities and explicit overrides from a captured environment.</summary>
    public sealed class CapabilityDetector : ICapabilityProvider {
        private readonly TerminalEnvironment _environment;
        private static readonly string[] CiVariables = {
            "CI", "GITHUB_ACTIONS", "TF_BUILD", "BUILD_BUILDID", "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION"
        };
        private static readonly string[] FullFonts = {
            "Cascadia", "JetBrains Mono", "Fira Code", "DejaVu Sans Mono", "Hack", "Iosevka",
            "Source Code Pro", "Ubuntu Mono", "Meslo", "Nerd Font"
        };

        /// <summary>Creates a detector; without a snapshot, captures the process once.</summary>
        /// <param name="environment">Detection inputs, or null to capture the process.</param>
        public CapabilityDetector(TerminalEnvironment? environment = null) {
            _environment = environment ?? TerminalEnvironment.Capture();
        }

        /// <inheritdoc />
        public TerminalProfile Detect() {
            TerminalEnvironment env = _environment;
            List<string> decisions = new List<string>();
            bool ci = false;
            foreach (string name in CiVariables) {
                string value = Get(name);
                if (value.Length > 0 && !Equal(value, "false") && value != "0") {
                    ci = true;
                    break;
                }
            }
            bool vt = env.IsWindows
                ? env.IsConsoleHandle && (env.VirtualTerminalEnabled ?? env.WindowsBuild >= 10586)
                : !env.IsOutputRedirected && Get("TERM").Length > 0;
            if (Equal(Get("TERM"), "dumb")) {
                vt = false;
            }
            OutputMode mode = DetectMode(ci, vt, out string modeReason);
            switch (Get("CONSOLEMONSTER_MODE").ToLowerInvariant()) {
                case "interactive":
                case "live": mode = OutputMode.Interactive; modeReason = "CONSOLEMONSTER_MODE"; break;
                case "stream": mode = OutputMode.Stream; modeReason = "CONSOLEMONSTER_MODE"; break;
                case "plain": mode = OutputMode.Plain; modeReason = "CONSOLEMONSTER_MODE"; break;
            }
            ColorDepth color = DetectColor(ci, vt, out string colorReason);
            GlyphTier glyphs = DetectGlyphs(mode, ci, out string glyphReason);
            bool explicitColor = true;
            switch (Get("CONSOLEMONSTER_COLOR").ToLowerInvariant()) {
                case "none": color = ColorDepth.None; break;
                case "16": color = ColorDepth.Ansi16; break;
                case "256": color = ColorDepth.Ansi256; break;
                case "truecolor":
                case "24bit": color = ColorDepth.TrueColor; break;
                default: explicitColor = false; break;
            }
            if (explicitColor) {
                colorReason = "CONSOLEMONSTER_COLOR";
            } else if (_environment.EnvironmentVariables.TryGetValue("NO_COLOR", out string? noColor) && !string.IsNullOrEmpty(noColor)) {
                color = ColorDepth.None;
                colorReason = "NO_COLOR";
            }
            switch (Get("CONSOLEMONSTER_GLYPHS").ToLowerInvariant()) {
                case "full": glyphs = GlyphTier.Full; glyphReason = "CONSOLEMONSTER_GLYPHS"; break;
                case "basic": glyphs = GlyphTier.Basic; glyphReason = "CONSOLEMONSTER_GLYPHS"; break;
                case "ascii": glyphs = GlyphTier.Ascii; glyphReason = "CONSOLEMONSTER_GLYPHS"; break;
            }
            if (mode == OutputMode.Plain) {
                color = ColorDepth.None;
                glyphs = GlyphTier.Ascii;
                colorReason = glyphReason = "plain mode";
            }
            int width = env.IsOutputRedirected ? Dimension("COLUMNS", TerminalProfile.DefaultWidth) : Positive(env.WindowWidth, TerminalProfile.DefaultWidth);
            int height = env.IsOutputRedirected ? Dimension("LINES", TerminalProfile.DefaultHeight) : Positive(env.WindowHeight, TerminalProfile.DefaultHeight);
            TerminalKind kind = DetectKind();
            decisions.Add("mode: " + mode + " (" + modeReason + ")");
            decisions.Add("colour: " + color + " (" + colorReason + ")");
            decisions.Add("glyphs: " + glyphs + " (" + glyphReason + ")");
            decisions.Add("VT: " + (vt ? "supported" : "unavailable"));
            decisions.Add("kind: " + kind);
            decisions.Add("size: " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + (env.IsOutputRedirected ? " (COLUMNS/LINES or defaults)" : " (visible window or defaults)"));
            return new TerminalProfile(color, glyphs, mode, width, height, kind, vt,
                env.IsOutputRedirected, env.IsConsoleHandle, ci, env.ConsoleFontFace, decisions.AsReadOnly());
        }

        private OutputMode DetectMode(bool ci, bool vt, out string reason) {
            if (Equal(Get("TERM"), "dumb")) { reason = "TERM=dumb"; return OutputMode.Plain; }
            if (ci) { reason = "CI environment"; return OutputMode.Stream; }
            if (_environment.IsOutputRedirected) { reason = "redirected stdout"; return OutputMode.Stream; }
            if (!vt) { reason = _environment.IsConsoleHandle ? "VT unavailable" : "no console handle/tty"; return OutputMode.Stream; }
            reason = "VT terminal";
            return OutputMode.Interactive;
        }

        private ColorDepth DetectColor(bool ci, bool vt, out string reason) {
            if (ci) { reason = "CI ANSI log support"; return ColorDepth.Ansi16; }
            if (_environment.IsOutputRedirected) { reason = "redirected stdout"; return ColorDepth.None; }
            if (!vt) { reason = "VT unavailable"; return ColorDepth.None; }
            if (Get("WT_SESSION").Length > 0) { reason = "WT_SESSION"; return ColorDepth.TrueColor; }
            if (Equal(Get("COLORTERM"), "truecolor") || Equal(Get("COLORTERM"), "24bit")) { reason = "COLORTERM"; return ColorDepth.TrueColor; }
            if (Equal(Get("TERM_PROGRAM"), "vscode")) { reason = "VS Code terminal"; return ColorDepth.TrueColor; }
            if (_environment.IsWindows && _environment.IsConsoleHandle) {
                if (_environment.WindowsBuild >= 14931) { reason = "Windows build >=14931"; return ColorDepth.TrueColor; }
                if (_environment.WindowsBuild >= 10586) { reason = "Windows build >=10586"; return ColorDepth.Ansi16; }
            }
            if (Contains(Get("TERM"), "256color")) { reason = "TERM 256color"; return ColorDepth.Ansi256; }
            string term = Get("TERM");
            if (Contains(term, "xterm") || Contains(term, "screen") || Contains(term, "tmux")
                || Contains(term, "rxvt") || Equal(term, "linux") || Equal(term, "ansi") || Equal(term, "vt100")) {
                reason = "TERM ANSI terminal"; return ColorDepth.Ansi16;
            }
            reason = "unknown colour support";
            return ColorDepth.None;
        }

        private GlyphTier DetectGlyphs(OutputMode mode, bool ci, out string reason) {
            if (mode != OutputMode.Interactive || ci) { reason = "stream/CI"; return GlyphTier.Basic; }
            if (Get("WT_SESSION").Length > 0 || Equal(Get("TERM_PROGRAM"), "vscode") || !_environment.IsWindows) {
                reason = "modern terminal"; return GlyphTier.Full;
            }
            string font = _environment.ConsoleFontFace ?? "unknown";
            reason = "conhost font '" + font + "'";
            if (_environment.ConsoleFontIsRaster || Equal(font, "Terminal")) { return GlyphTier.Ascii; }
            foreach (string fullFont in FullFonts) {
                if (Contains(font, fullFont)) { return GlyphTier.Full; }
            }
            string[] fontWords = font.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in fontWords) {
                if (Equal(word, "NF")) { return GlyphTier.Full; }
            }
            return GlyphTier.Basic;
        }

        private TerminalKind DetectKind() {
            if (_environment.IsOutputRedirected) { return TerminalKind.Redirected; }
            if (_environment.IsWindows && !_environment.IsConsoleHandle) {
                return Get("PSISE").Length > 0 || Get("PSISE_HOST").Length > 0 ? TerminalKind.PowerShellIse : TerminalKind.Unknown;
            }
            if (Get("WT_SESSION").Length > 0) { return TerminalKind.WindowsTerminal; }
            if (Equal(Get("TERM_PROGRAM"), "vscode")) { return TerminalKind.VisualStudioCode; }
            if (_environment.IsWindows && _environment.IsConsoleHandle) { return TerminalKind.Conhost; }
            return Get("TERM").Length > 0 ? TerminalKind.Xterm : TerminalKind.Unknown;
        }

        private string Get(string key) => _environment.EnvironmentVariables.TryGetValue(key, out string? value) ? (value ?? string.Empty).Trim() : string.Empty;
        private int Dimension(string name, int fallback) => int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Positive(value, fallback) : fallback;
        private static int Positive(int value, int fallback) => value > 0 ? value : fallback;
        private static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        private static bool Contains(string value, string part) => value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
