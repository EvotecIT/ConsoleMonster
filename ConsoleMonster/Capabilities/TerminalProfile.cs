using System;
using System.Globalization;
using System.Collections.Generic;
using ConsoleMonster.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// An immutable description of what the current terminal can do. Produced by an
    /// <see cref="ICapabilityProvider"/>, overridable by code, environment variables
    /// (<c>CONSOLEMONSTER_COLOR</c>, <c>CONSOLEMONSTER_GLYPHS</c>, <c>CONSOLEMONSTER_MODE</c>, <c>CONSOLEMONSTER_THEME</c>, <c>CONSOLEMONSTER_HYPERLINKS</c>, <c>NO_COLOR</c>) and configuration.
    /// Rendering is a pure function of the widget, its state, this profile and the theme.
    /// </summary>
    public sealed class TerminalProfile {

        /// <summary>The default width used when the real width cannot be determined (redirected output).</summary>
        public const int DefaultWidth = 100;

        /// <summary>The default height used when the real height cannot be determined.</summary>
        public const int DefaultHeight = 40;

        private static readonly IReadOnlyList<string> NoDecisions = new string[0];
        private readonly bool _supportsHyperlinks;

        /// <summary>Creates a profile through the original constructor signature, retained for compiled consumers.</summary>
        /// <param name="colorDepth">Colour fidelity.</param>
        /// <param name="glyphs">Glyph tier.</param>
        /// <param name="mode">Output mode.</param>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        /// <param name="kind">Detected terminal kind.</param>
        /// <param name="supportsVirtualTerminal">Whether VT sequences are understood.</param>
        /// <param name="isOutputRedirected">Whether standard output is redirected.</param>
        /// <param name="isConsoleHandle">Whether output goes to a real Windows console handle.</param>
        /// <param name="isContinuousIntegration">Whether a CI environment was detected.</param>
        /// <param name="fontFace">The console font face, when known.</param>
        /// <param name="decisions">Notes explaining the capability decisions.</param>
        /// <param name="widthProfile">Display width assumptions.</param>
        /// <param name="probeWidths">Whether a live width probe is permitted.</param>
        /// <param name="themePreference">Requested terminal theme preference.</param>
        /// <param name="culture">Culture for numbers and dates, or null for the current culture.</param>
        public TerminalProfile(ColorDepth colorDepth, GlyphTier glyphs, OutputMode mode, int width, int height,
            TerminalKind kind, bool supportsVirtualTerminal, bool isOutputRedirected, bool isConsoleHandle,
            bool isContinuousIntegration, string? fontFace, IReadOnlyList<string>? decisions,
            TerminalWidthProfile? widthProfile, bool probeWidths, ThemePreference themePreference, CultureInfo? culture)
            : this(colorDepth, glyphs, mode, width, height, kind, supportsVirtualTerminal, isOutputRedirected,
                isConsoleHandle, isContinuousIntegration, fontFace, decisions, widthProfile, probeWidths, themePreference, culture, null) {
        }

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
        /// <param name="culture">The culture for numbers and dates in rendered output, or null for the current culture.</param>
        /// <param name="supportsHyperlinks">OSC 8 support, or null to infer it from the terminal kind. Redirected, CI, noninteractive and non-VT output always disable links.</param>
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
            ThemePreference themePreference = ThemePreference.Auto,
            CultureInfo? culture = null,
            bool? supportsHyperlinks = null) {
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
            Culture = culture;
            _supportsHyperlinks = supportsHyperlinks ?? (kind == TerminalKind.WindowsTerminal || kind == TerminalKind.VisualStudioCode
                || kind == TerminalKind.ITerm2 || kind == TerminalKind.WezTerm);
        }

        /// <summary>Colour fidelity.</summary>
        public ColorDepth ColorDepth { get; }

        /// <summary>The per-render foreground used when a style requests the terminal default.
        /// Defaults to <see cref="Color.Default"/>; the profile never reads or changes host colors itself.</summary>
        public Color DefaultForeground { get; private set; }

        /// <summary>The per-render background used when a style requests the terminal default.
        /// Defaults to <see cref="Color.Default"/>; color-disabled output ignores this baseline.</summary>
        public Color DefaultBackground { get; private set; }

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

        /// <summary>
        /// Whether this output may emit OSC 8 hyperlinks. Requires interactive VT output and detected or explicitly
        /// enabled support; redirected output and CI always return false, including when support was overridden.
        /// </summary>
        public bool SupportsHyperlinks => _supportsHyperlinks && SupportsVirtualTerminal && Mode == OutputMode.Interactive
            && !IsOutputRedirected && !IsContinuousIntegration;

        /// <summary>The detected or explicitly configured support, preserved when copying a profile into another mode.</summary>
        internal bool HyperlinkSupport => _supportsHyperlinks;

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

        /// <summary>
        /// The culture for numbers and dates in rendered output (thousand and decimal separators, date formats), or null to
        /// follow the current culture at render time, as PowerShell does. Set by <c>CONSOLEMONSTER_CULTURE</c> or
        /// <see cref="WithCulture"/>. Built-in labels follow <see cref="Language"/>.
        /// </summary>
        public CultureInfo? Culture { get; }

        /// <summary>
        /// The language of built-in labels (prompt keys, status words, "Showing 5 of 20"), or null for the current UI
        /// culture (<c>$PSUICulture</c> in PowerShell), unless <c>CONSOLEMONSTER_LANGUAGE</c> names one. English, Polish,
        /// German, Spanish, French and Italian are built in; other languages fall back to English. Set with
        /// <see cref="WithLanguage"/>.
        /// </summary>
        public CultureInfo? Language { get; private set; }

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

        /// <summary>Returns a copy with explicit default colors for this rendering, including style and output resets.</summary>
        /// <param name="foreground">The supplied host foreground, or <see cref="Color.Default"/> to keep VT defaults.</param>
        /// <param name="background">The supplied host background, or <see cref="Color.Default"/> to keep VT defaults.</param>
        /// <returns>A new profile. Explicit style colors still take precedence; Plain and color-disabled output emit no SGR.</returns>
        public TerminalProfile WithDefaultColors(Color foreground, Color background) {
            TerminalProfile copy = Copy();
            copy.DefaultForeground = foreground;
            copy.DefaultBackground = background;
            return copy;
        }

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

        /// <summary>Returns a copy that renders numbers and dates with the given culture.</summary>
        /// <param name="culture">The culture, or null to follow the current culture.</param>
        /// <returns>The copy.</returns>
        public TerminalProfile WithCulture(CultureInfo? culture) {
            return new TerminalProfile(ColorDepth, Glyphs, Mode, Width, Height, Kind, SupportsVirtualTerminal, IsOutputRedirected,
                IsConsoleHandle, IsContinuousIntegration, FontFace, Decisions, WidthProfile, ProbeWidths, ThemePreference, culture, _supportsHyperlinks) {
                Language = Language, DefaultForeground = DefaultForeground, DefaultBackground = DefaultBackground
            };
        }

        /// <summary>Returns a copy whose built-in labels use the given language (see <see cref="Language"/>).</summary>
        /// <param name="language">The language, or null to follow the current UI culture.</param>
        /// <returns>The copy.</returns>
        public TerminalProfile WithLanguage(CultureInfo? language) {
            TerminalProfile copy = Copy();
            copy.Language = language;
            return copy;
        }

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

        /// <summary>Returns a copy with OSC 8 support enabled or disabled. Output, CI, mode and VT guards still apply.</summary>
        /// <param name="supportsHyperlinks">Whether the terminal supports hyperlinks.</param>
        /// <returns>A new profile.</returns>
        public TerminalProfile WithHyperlinks(bool supportsHyperlinks) => Copy(supportsHyperlinks: supportsHyperlinks);

        /// <inheritdoc />
        public override string ToString() {
            return Kind + " " + Width + "x" + Height + " " + Mode + " " + ColorDepth + " " + Glyphs;
        }

        private TerminalProfile Copy(ColorDepth? colorDepth = null, GlyphTier? glyphs = null, OutputMode? mode = null, int? width = null, int? height = null, IReadOnlyList<string>? decisions = null, TerminalWidthProfile? widthProfile = null, ThemePreference? themePreference = null, bool? supportsHyperlinks = null) {
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
                themePreference ?? ThemePreference,
                Culture, supportsHyperlinks ?? _supportsHyperlinks) {
                Language = Language, DefaultForeground = DefaultForeground, DefaultBackground = DefaultBackground
            };
        }
    }
}
