using System;
using System.Collections.Generic;
using ConsoleMonster.Text;

namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// An immutable description of what the current terminal can do. Produced by an
    /// <see cref="ICapabilityProvider"/>, overridable by code, environment variables
    /// (<c>CONSOLEMONSTER_COLOR</c>, <c>CONSOLEMONSTER_GLYPHS</c>, <c>CONSOLEMONSTER_MODE</c>, <c>CONSOLEMONSTER_THEME</c>, <c>NO_COLOR</c>) and configuration.
    /// Rendering is a pure function of the widget, its state, this profile and the theme.
    /// </summary>
    public sealed class TerminalProfile {

        /// <summary>The default width used when the real width cannot be determined (redirected output).</summary>
        public const int DefaultWidth = 120;

        /// <summary>The default height used when the real height cannot be determined.</summary>
        public const int DefaultHeight = 40;

        private static readonly IReadOnlyList<string> NoDecisions = new string[0];

        /// <summary>
        /// Creates a profile with explicit values. Tests and fixed-output scenarios use this directly.
        /// </summary>
        /// <param name="colorDepth">Colour fidelity.</param>
        /// <param name="glyphs">Glyph tier.</param>
        /// <param name="mode">Output mode.</param>
        /// <param name="width">Width in cells; values below 1 fall back to <see cref="DefaultWidth"/>.</param>
        /// <param name="height">Height in cells; values below 1 fall back to <see cref="DefaultHeight"/>.</param>
        /// <param name="kind">Detected terminal kind.</param>
        /// <param name="supportsVirtualTerminal">Whether VT sequences are understood.</param>
        /// <param name="isOutputRedirected">Whether standard output is redirected.</param>
        /// <param name="isConsoleHandle">Whether output goes to a real Windows console handle (written with WriteConsoleW).</param>
        /// <param name="isContinuousIntegration">Whether a CI environment was detected.</param>
        /// <param name="fontFace">The console font face, when known.</param>
        /// <param name="decisions">Human-readable notes explaining how each value was chosen.</param>
        /// <param name="widthProfile">Configured or derived display width assumptions.</param>
        /// <param name="probeWidths">Whether a live width probe is permitted.</param>
        /// <param name="themePreference">Requested terminal theme preference.</param>
        public TerminalProfile(
            ColorDepth colorDepth,
            GlyphTier glyphs,
            OutputMode mode,
            int width = DefaultWidth,
            int height = DefaultHeight,
            TerminalKind kind = TerminalKind.Unknown,
            bool supportsVirtualTerminal = false,
            bool isOutputRedirected = false,
            bool isConsoleHandle = false,
            bool isContinuousIntegration = false,
            string? fontFace = null,
            IReadOnlyList<string>? decisions = null,
            TerminalWidthProfile? widthProfile = null,
            bool probeWidths = false,
            ThemePreference themePreference = ThemePreference.Auto) {
            ColorDepth = colorDepth;
            Glyphs = glyphs;
            Mode = mode;
            Width = width > 0 ? width : DefaultWidth;
            Height = height > 0 ? height : DefaultHeight;
            Kind = kind;
            SupportsVirtualTerminal = supportsVirtualTerminal;
            IsOutputRedirected = isOutputRedirected;
            IsConsoleHandle = isConsoleHandle;
            IsContinuousIntegration = isContinuousIntegration;
            FontFace = fontFace;
            Decisions = decisions ?? NoDecisions;
            WidthProfile = widthProfile ?? TerminalWidthProfile.Standard;
            ProbeWidths = probeWidths;
            ThemePreference = themePreference;
        }

        /// <summary>Colour fidelity.</summary>
        public ColorDepth ColorDepth { get; }

        /// <summary>Glyph tier.</summary>
        public GlyphTier Glyphs { get; }

        /// <summary>Output mode.</summary>
        public OutputMode Mode { get; }

        /// <summary>Width in cells.</summary>
        public int Width { get; }

        /// <summary>Height in cells.</summary>
        public int Height { get; }

        /// <summary>Detected terminal kind.</summary>
        public TerminalKind Kind { get; }

        /// <summary>Whether VT escape sequences are understood.</summary>
        public bool SupportsVirtualTerminal { get; }

        /// <summary>Whether standard output is redirected to a file, pipe or log.</summary>
        public bool IsOutputRedirected { get; }

        /// <summary>Whether output goes to a real Windows console handle (UTF-16 through WriteConsoleW).</summary>
        public bool IsConsoleHandle { get; }

        /// <summary>Whether a CI environment was detected.</summary>
        public bool IsContinuousIntegration { get; }

        /// <summary>The console font face, when known (conhost through GetCurrentConsoleFontEx).</summary>
        public string? FontFace { get; }

        /// <summary>Human-readable notes explaining how each value was chosen (shown by <c>Get-ConsoleCapability</c>).</summary>
        public IReadOnlyList<string> Decisions { get; }

        /// <summary>The configured or derived Unicode display width profile.</summary>
        public TerminalWidthProfile WidthProfile { get; }

        /// <summary>Whether an interactive session may probe terminal widths.</summary>
        public bool ProbeWidths { get; }

        /// <summary>The requested light, dark, or automatically detected theme preference.</summary>
        public ThemePreference ThemePreference { get; }

        /// <summary>Whether the profile allows live redraw (interactive mode with VT support).</summary>
        public bool IsLive => Mode == OutputMode.Interactive && SupportsVirtualTerminal;

        /// <summary>
        /// A fixed profile for tests, snapshots and HTML-free documentation captures.
        /// </summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        /// <param name="colorDepth">Colour fidelity.</param>
        /// <param name="glyphs">Glyph tier.</param>
        /// <param name="mode">Output mode.</param>
        /// <returns>A profile with VT support when the mode is interactive.</returns>
        public static TerminalProfile Fixed(int width, int height, ColorDepth colorDepth = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full, OutputMode mode = OutputMode.Interactive) {
            return new TerminalProfile(colorDepth, glyphs, mode, width, height, TerminalKind.Unknown, mode == OutputMode.Interactive);
        }

        /// <summary>Returns a copy with a different colour depth.</summary>
        /// <param name="colorDepth">The new colour depth.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithColorDepth(ColorDepth colorDepth) => Copy(colorDepth: colorDepth);

        /// <summary>Returns a copy with a different glyph tier.</summary>
        /// <param name="glyphs">The new glyph tier.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithGlyphs(GlyphTier glyphs) => Copy(glyphs: glyphs);

        /// <summary>Returns a copy with a different output mode.</summary>
        /// <param name="mode">The new mode.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithMode(OutputMode mode) => Copy(mode: mode);

        /// <summary>Returns a copy with a different theme preference.</summary>
        /// <param name="themePreference">The requested preference.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithThemePreference(ThemePreference themePreference) => Copy(themePreference: themePreference);

        /// <summary>Returns a copy with a different size (used on resize).</summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithSize(int width, int height) => Copy(width: width, height: height);

        /// <summary>Returns a copy with an extra decision note appended.</summary>
        /// <param name="decision">The note.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithDecision(string decision) {
            if (decision == null) {
                throw new ArgumentNullException(nameof(decision));
            }
            List<string> list = new List<string>(Decisions) { decision };
            return Copy(decisions: list);
        }

        /// <summary>Returns a copy with a different terminal width profile.</summary>
        public TerminalProfile WithWidthProfile(TerminalWidthProfile profile) => Copy(widthProfile: profile ?? throw new ArgumentNullException(nameof(profile)));

        /// <inheritdoc />
        public override string ToString() {
            return Kind + " " + Width + "x" + Height + " " + Mode + " " + ColorDepth + " " + Glyphs;
        }

        private TerminalProfile Copy(ColorDepth? colorDepth = null, GlyphTier? glyphs = null, OutputMode? mode = null, int? width = null, int? height = null, IReadOnlyList<string>? decisions = null, TerminalWidthProfile? widthProfile = null, ThemePreference? themePreference = null) {
            return new TerminalProfile(
                colorDepth ?? ColorDepth,
                glyphs ?? Glyphs,
                mode ?? Mode,
                width ?? Width,
                height ?? Height,
                Kind,
                SupportsVirtualTerminal,
                IsOutputRedirected,
                IsConsoleHandle,
                IsContinuousIntegration,
                FontFace,
                decisions ?? Decisions,
                widthProfile ?? WidthProfile,
                ProbeWidths,
                themePreference ?? ThemePreference);
        }
    }
}
