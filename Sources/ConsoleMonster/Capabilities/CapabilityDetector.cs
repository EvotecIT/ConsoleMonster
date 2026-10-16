using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Text;

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
                case "live":
                    mode = OutputMode.Interactive;
                    modeReason = "CONSOLEMONSTER_MODE";
                    break;
                case "stream":
                    mode = OutputMode.Stream;
                    modeReason = "CONSOLEMONSTER_MODE";
                    break;
                case "plain":
                    mode = OutputMode.Plain;
                    modeReason = "CONSOLEMONSTER_MODE";
                    break;
            }
            ColorDepth color = DetectColor(ci, vt, out string colorReason);
            GlyphTier glyphs = DetectGlyphs(mode, ci, out string glyphReason);
            bool explicitColor = true;
            switch (Get("CONSOLEMONSTER_COLOR").ToLowerInvariant()) {
                case "none":
                    color = ColorDepth.None;
                    break;
                case "16":
                    color = ColorDepth.Ansi16;
                    break;
                case "256":
                    color = ColorDepth.Ansi256;
                    break;
                case "truecolor":
                case "24bit":
                    color = ColorDepth.TrueColor;
                    break;
                default:
                    explicitColor = false;
                    break;
            }
            if (explicitColor) {
                colorReason = "CONSOLEMONSTER_COLOR";
            } else if (_environment.EnvironmentVariables.TryGetValue("NO_COLOR", out string? noColor) && !string.IsNullOrEmpty(noColor)) {
                color = ColorDepth.None;
                colorReason = "NO_COLOR";
            }
            switch (Get("CONSOLEMONSTER_GLYPHS").ToLowerInvariant()) {
                case "full":
                    glyphs = GlyphTier.Full;
                    glyphReason = "CONSOLEMONSTER_GLYPHS";
                    break;
                case "basic":
                    glyphs = GlyphTier.Basic;
                    glyphReason = "CONSOLEMONSTER_GLYPHS";
                    break;
                case "ascii":
                    glyphs = GlyphTier.Ascii;
                    glyphReason = "CONSOLEMONSTER_GLYPHS";
                    break;
            }
            ThemePreference themePreference = ThemePreference.Auto;
            string themeValue = Get("CONSOLEMONSTER_THEME");
            switch (themeValue.ToLowerInvariant()) {
                case "light":
                    themePreference = ThemePreference.Light;
                    break;
                case "dark":
                    themePreference = ThemePreference.Dark;
                    break;
                case "":
                case "auto":
                    break;
                default:
                    decisions.Add("CONSOLEMONSTER_THEME: ignored invalid value '" + themeValue + "'");
                    break;
            }
            if (mode == OutputMode.Plain) {
                color = ColorDepth.None;
                glyphs = GlyphTier.Ascii;
                colorReason = glyphReason = "plain mode";
            }
            TerminalWidthProfile widths = TerminalWidthProfile.Standard;
            bool emojiAuto = ParseWidth("CONSOLEMONSTER_EMOJI_WIDTH", ref widths, true, decisions);
            bool ambiguousAuto = ParseWidth("CONSOLEMONSTER_AMBIGUOUS_WIDTH", ref widths, false, decisions);
            bool probeWidths = mode == OutputMode.Interactive && !_environment.IsOutputRedirected && !_environment.IsInputRedirected && (emojiAuto || ambiguousAuto);
            if (widths.AmbiguousWidth == 2 && glyphs != GlyphTier.Ascii) {
                glyphs = GlyphTier.Ascii;
                glyphReason = "ambiguous width 2";
                decisions.Add("glyphs downgraded: ambiguous width 2");
            }
            int width = env.IsOutputRedirected ? Dimension("COLUMNS", TerminalProfile.DefaultWidth) : Positive(env.WindowWidth, TerminalProfile.DefaultWidth);
            int height = env.IsOutputRedirected ? Dimension("LINES", TerminalProfile.DefaultHeight) : Positive(env.WindowHeight, TerminalProfile.DefaultHeight);
            TerminalKind kind = DetectKind();
            decisions.Add("mode: " + mode + " (" + modeReason + ")");
            decisions.Add("colour: " + color + " (" + colorReason + ")");
            decisions.Add("glyphs: " + glyphs + " (" + glyphReason + ")");
            decisions.Add("VT: " + (vt ? "supported" : "unavailable"));
            decisions.Add("kind: " + kind);
            decisions.Add("width profile: " + widths);
            decisions.Add("probe widths: " + probeWidths);
            decisions.Add("theme preference: " + themePreference + " (CONSOLEMONSTER_THEME)");
            CultureInfo? culture = ParseCulture(Get("CONSOLEMONSTER_CULTURE"), decisions);
            decisions.Add("size: " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + (env.IsOutputRedirected ? " (COLUMNS/LINES or defaults)" : " (visible window or defaults)"));
            return new TerminalProfile(color, glyphs, mode, width, height, kind, vt,
                env.IsOutputRedirected, env.IsConsoleHandle, ci, env.ConsoleFontFace, decisions.AsReadOnly(), widths, probeWidths, themePreference, culture);
        }

        // CONSOLEMONSTER_CULTURE: a culture name (en-US, pl-PL), "invariant", or empty/"current" for the current culture.
        private static CultureInfo? ParseCulture(string value, List<string> decisions) {
            if (value.Length == 0 || Equal(value, "current")) {
                decisions.Add("culture: current (" + CultureInfo.CurrentCulture.Name + ") at render time");
                return null;
            }
            if (Equal(value, "invariant")) {
                decisions.Add("culture: invariant (CONSOLEMONSTER_CULTURE)");
                return CultureInfo.InvariantCulture;
            }
            try {
#if NET5_0_OR_GREATER
                // ICU accepts any well-formed tag as a custom culture; only predefined cultures are meaningful here.
                CultureInfo culture = CultureInfo.GetCultureInfo(value, predefinedOnly: true);
#else
                CultureInfo culture = CultureInfo.GetCultureInfo(value);
#endif
                if (string.Equals(culture.ThreeLetterWindowsLanguageName, "ZZZ", StringComparison.Ordinal)) {
                    // Windows synthesises "unknown locale" cultures for well-formed but unknown names.
                    throw new CultureNotFoundException(nameof(value), value, "Unknown culture.");
                }
                decisions.Add("culture: " + culture.Name + " (CONSOLEMONSTER_CULTURE)");
                return culture;
            } catch (CultureNotFoundException) {
                decisions.Add("CONSOLEMONSTER_CULTURE: ignored unknown culture '" + value + "'; using the current culture");
                return null;
            }
        }

        /// <summary>Re-evaluates font-dependent glyph decisions for an existing profile.</summary>
        /// <param name="profile">The profile to refresh.</param>
        /// <param name="fontFace">The current console font face, or null when unavailable.</param>
        /// <param name="fontIsRaster">Whether the current font is a raster font.</param>
        /// <returns>A profile with refreshed font and glyph decisions.</returns>
        public TerminalProfile RefreshFont(TerminalProfile profile, string? fontFace, bool fontIsRaster = false) {
            if (profile == null) {
                throw new ArgumentNullException(nameof(profile));
            }
            string font = fontFace ?? "unknown";
            GlyphTier glyphs = DetectGlyphs(profile.Mode, profile.IsContinuousIntegration, font, fontIsRaster, out string reason);
            switch (Get("CONSOLEMONSTER_GLYPHS").ToLowerInvariant()) {
                case "full":
                    glyphs = GlyphTier.Full;
                    reason = "CONSOLEMONSTER_GLYPHS";
                    break;
                case "basic":
                    glyphs = GlyphTier.Basic;
                    reason = "CONSOLEMONSTER_GLYPHS";
                    break;
                case "ascii":
                    glyphs = GlyphTier.Ascii;
                    reason = "CONSOLEMONSTER_GLYPHS";
                    break;
            }
            if (profile.Mode == OutputMode.Plain) {
                glyphs = GlyphTier.Ascii;
                reason = "plain mode";
            }
            List<string> decisions = new List<string>(profile.Decisions) {
                "font refreshed: " + font,
                "glyphs: " + glyphs + " (" + reason + ")"
            };
            if (profile.WidthProfile.AmbiguousWidth == 2 && glyphs != GlyphTier.Ascii) {
                glyphs = GlyphTier.Ascii;
                decisions.Add("glyphs downgraded: ambiguous width 2");
            }
            return new TerminalProfile(profile.ColorDepth, glyphs, profile.Mode, profile.Width, profile.Height,
                profile.Kind, profile.SupportsVirtualTerminal, profile.IsOutputRedirected, profile.IsConsoleHandle,
                profile.IsContinuousIntegration, fontFace, decisions.AsReadOnly(), profile.WidthProfile, profile.ProbeWidths,
                profile.ThemePreference, profile.Culture);
        }

        private bool ParseWidth(string name, ref TerminalWidthProfile profile, bool emoji, List<string> decisions) {
            string value = Get(name);
            if (value.Length == 0 || Equal(value, "auto")) {
                return true;
            }
            if (value == "1" || value == "2") {
                profile = (emoji ? profile.WithEmojiWidth(int.Parse(value, CultureInfo.InvariantCulture)) : profile.WithAmbiguousWidth(int.Parse(value, CultureInfo.InvariantCulture))).WithSource("environment");
                decisions.Add(name + ": " + value);
                return false;
            }
            decisions.Add(name + ": ignored invalid value '" + value + "'");
            return false;
        }

        private OutputMode DetectMode(bool ci, bool vt, out string reason) {
            if (Equal(Get("TERM"), "dumb")) {
                reason = "TERM=dumb";
                return OutputMode.Plain;
            }
            if (ci) {
                reason = "CI environment";
                return OutputMode.Stream;
            }
            if (_environment.IsOutputRedirected) {
                reason = "redirected stdout";
                return OutputMode.Stream;
            }
            if (!vt) {
                reason = _environment.IsConsoleHandle ? "VT unavailable" : "no console handle/tty";
                return OutputMode.Stream;
            }
            reason = "VT terminal";
            return OutputMode.Interactive;
        }

        private ColorDepth DetectColor(bool ci, bool vt, out string reason) {
            if (ci) {
                reason = "CI ANSI log support";
                return ColorDepth.Ansi16;
            }
            if (_environment.IsOutputRedirected) {
                reason = "redirected stdout";
                return ColorDepth.None;
            }
            if (!vt) {
                reason = "VT unavailable";
                return ColorDepth.None;
            }
            if (Get("WT_SESSION").Length > 0) {
                reason = "WT_SESSION";
                return ColorDepth.TrueColor;
            }
            if (Equal(Get("COLORTERM"), "truecolor") || Equal(Get("COLORTERM"), "24bit")) {
                reason = "COLORTERM";
                return ColorDepth.TrueColor;
            }
            if (Equal(Get("TERM_PROGRAM"), "vscode")) {
                reason = "VS Code terminal";
                return ColorDepth.TrueColor;
            }
            if (_environment.IsWindows && _environment.IsConsoleHandle) {
                if (_environment.WindowsBuild >= 14931) {
                    reason = "Windows build >=14931";
                    return ColorDepth.TrueColor;
                }
                if (_environment.WindowsBuild >= 10586) {
                    reason = "Windows build >=10586";
                    return ColorDepth.Ansi16;
                }
            }
            if (Contains(Get("TERM"), "256color")) {
                reason = "TERM 256color";
                return ColorDepth.Ansi256;
            }
            string term = Get("TERM");
            if (Contains(term, "xterm") || Contains(term, "screen") || Contains(term, "tmux")
                || Contains(term, "rxvt") || Equal(term, "linux") || Equal(term, "ansi") || Equal(term, "vt100")) {
                reason = "TERM ANSI terminal";
                return ColorDepth.Ansi16;
            }
            reason = "unknown colour support";
            return ColorDepth.None;
        }

        private GlyphTier DetectGlyphs(OutputMode mode, bool ci, out string reason) {
            return DetectGlyphs(mode, ci, _environment.ConsoleFontFace, _environment.ConsoleFontIsRaster, out reason);
        }

        private GlyphTier DetectGlyphs(OutputMode mode, bool ci, string? consoleFontFace, bool consoleFontIsRaster, out string reason) {
            if (mode != OutputMode.Interactive || ci) {
                reason = "stream/CI";
                return GlyphTier.Basic;
            }
            if (Get("WT_SESSION").Length > 0 || Equal(Get("TERM_PROGRAM"), "vscode") || !_environment.IsWindows) {
                reason = "modern terminal";
                return GlyphTier.Full;
            }
            string font = consoleFontFace ?? "unknown";
            reason = "conhost font '" + font + "'";
            if (consoleFontIsRaster || Equal(font, "Terminal")) {
                return GlyphTier.Ascii;
            }
            foreach (string fullFont in FullFonts) {
                if (Contains(font, fullFont)) {
                    return GlyphTier.Full;
                }
            }
            string[] fontWords = font.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in fontWords) {
                if (Equal(word, "NF")) {
                    return GlyphTier.Full;
                }
            }
            return GlyphTier.Basic;
        }

        private TerminalKind DetectKind() {
            if (_environment.IsOutputRedirected) {
                return TerminalKind.Redirected;
            }
            if (_environment.IsWindows && !_environment.IsConsoleHandle) {
                return Get("PSISE").Length > 0 || Get("PSISE_HOST").Length > 0 ? TerminalKind.PowerShellIse : TerminalKind.Unknown;
            }
            if (Get("WT_SESSION").Length > 0) {
                return TerminalKind.WindowsTerminal;
            }
            if (Equal(Get("TERM_PROGRAM"), "vscode")) {
                return TerminalKind.VisualStudioCode;
            }
            if (_environment.IsWindows && _environment.IsConsoleHandle) {
                return TerminalKind.Conhost;
            }
            return Get("TERM").Length > 0 ? TerminalKind.Xterm : TerminalKind.Unknown;
        }

        private string Get(string key) => _environment.EnvironmentVariables.TryGetValue(key, out string? value) ? (value ?? string.Empty).Trim() : string.Empty;
        private int Dimension(string name, int fallback) => int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Positive(value, fallback) : fallback;
        private static int Positive(int value, int fallback) => value > 0 ? value : fallback;
        private static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        private static bool Contains(string value, string part) => value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
