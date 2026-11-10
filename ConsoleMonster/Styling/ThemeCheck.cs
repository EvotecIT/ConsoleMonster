using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;

namespace ConsoleMonster.Styling {
    /// <summary>Checks theme text, status colours, markers and critical fills without guessing a terminal's palette.</summary>
    public static partial class ThemeCheck {
        private static readonly ThemeToken[] Statuses = { ThemeToken.Pass, ThemeToken.Warn, ThemeToken.Fail };
        private static readonly StatusKind[] StatusKinds = { StatusKind.Pass, StatusKind.Warn, StatusKind.Fail };
        // Text-bearing status tokens, plus the body and secondary text used beside them. Tracks, borders and chart
        // series are not text; Surface is chrome, not the page background.
        private static readonly ThemeToken[] TextTokens = {
            ThemeToken.Text, ThemeToken.Muted, ThemeToken.Pass, ThemeToken.Warn, ThemeToken.Fail,
            ThemeToken.Info, ThemeToken.Skip, ThemeToken.Running, ThemeToken.Pending,
            ThemeToken.Low, ThemeToken.Critical, ThemeToken.Gap
        };

        /// <summary>Measures WCAG 4.5:1 text contrast and CIELAB distance 25 in normal vision and Machado severity-1 simulations, then checks markers and filled Critical.</summary>
        /// <param name="theme">Any theme; resolved palette indexes and terminal-default foregrounds remain unknown.</param>
        /// <param name="options">Page background and marker rendering conditions. No terminal queries are sent.</param>
        /// <returns>An immutable report. Unknown measurements prevent Passed, but are distinct from failures.</returns>
        public static ThemeCheckReport Run(ITheme theme, ThemeCheckOptions? options = null) {
            if (theme == null) { throw new ArgumentNullException(nameof(theme)); }
            options = options ?? new ThemeCheckOptions();
            if (theme is Theme following && following.FollowsTerminal && global::ConsoleMonster.Output.TerminalPalette.Current is TerminalScheme palette) { theme = following.ResolvePalette(palette); }
            Color page = options.Background ?? Theme.Page(theme).Background;
            List<ThemeCheckResult> results = new List<ThemeCheckResult>();
            foreach (ThemeToken token in TextTokens) {
                Style style = theme.Resolve(token, ColorDepth.TrueColor);
                Color foreground = TextColour(style, page);
                Color background = FillColour(style, page);
                double? ratio = IsRgb(foreground) && IsRgb(background) ? Contrast(foreground, background) : (double?)null;
                results.Add(Result(ThemeCheckKind.Contrast, new[] { token }, ratio, 4.5,
                    token + " text is readable.", token + " text is too faint", UnknownContrast(token, style, foreground, background)));
            }
            for (int vision = 0; vision < Simulations.Length; vision++) {
                for (int i = 0; i < Statuses.Length; i++) {
                    for (int j = i + 1; j < Statuses.Length; j++) {
                        Color a = TextColour(theme.Resolve(Statuses[i], ColorDepth.TrueColor), page);
                        Color b = TextColour(theme.Resolve(Statuses[j], ColorDepth.TrueColor), page);
                        double? distance = IsRgb(a) && IsRgb(b) ? Distance(Lab(a, Simulations[vision]), Lab(b, Simulations[vision])) : (double?)null;
                        results.Add(Result(ThemeCheckKind.ColourVision, new[] { Statuses[i], Statuses[j] }, distance, 25,
                            ThemeCheckText.Pair(new[] { Statuses[i], Statuses[j] }) + " are apart " + ThemeCheckText.Conditions(new[] { VisionNames[vision] }),
                            ThemeCheckText.Pair(new[] { Statuses[i], Statuses[j] }) + " look alike " + ThemeCheckText.Conditions(new[] { VisionNames[vision] }),
                            ThemeCheckText.Pair(new[] { Statuses[i], Statuses[j] }) + " colours can't be compared " + ThemeCheckText.Conditions(new[] { VisionNames[vision] }), VisionNames[vision]));
                    }
                }
            }
            CheckMarkers(theme, options, results);
            CheckFill(theme, page, results);
            return new ThemeCheckReport(theme.Name, results, (theme as Theme)?.AdjustedTokens);
        }

        // Terminals choose their own faint intensity. An undimmed RGB value cannot establish the displayed contrast
        // or separation, even though an export renderer has its own deterministic dim approximation.
        private static Color TextColour(Style style, Color page) => (style.Decoration & TextDecoration.Dim) != 0 ? Color.Default
            : (style.Decoration & TextDecoration.Reverse) == 0 ? style.Color : style.BackgroundColor.IsDefault ? page : style.BackgroundColor;

        private static Color FillColour(Style style, Color page) => (style.Decoration & TextDecoration.Reverse) != 0
            ? style.Color : style.BackgroundColor.IsDefault ? page : style.BackgroundColor;

        private static string UnknownContrast(ThemeToken token, Style style, Color foreground, Color background) {
            if ((style.Decoration & TextDecoration.Dim) != 0) { return token + " is dimmed by the terminal, so its contrast can't be measured"; }
            if (foreground.IsDefault) { return token + " uses the terminal's own colour, so its contrast can't be measured"; }
            if (background.IsDefault) { return token + " has an unknown background, so its contrast can't be measured"; }
            return token + " uses the terminal palette, so its contrast can't be measured";
        }

        private static bool IsRgb(Color colour) => colour.Kind == ColorKind.Rgb;

        private static ThemeCheckResult Result(ThemeCheckKind check, ThemeToken[] tokens, double? value, double required, string pass, string fail, string unknown, string? vision = null) =>
            new ThemeCheckResult(check, tokens, !value.HasValue ? ThemeCheckOutcome.Unknown : value.Value >= required ? ThemeCheckOutcome.Pass : ThemeCheckOutcome.Fail,
                value, required, !value.HasValue ? unknown : value.Value >= required ? pass : fail, vision);

        private static void CheckMarkers(ITheme theme, ThemeCheckOptions options, List<ThemeCheckResult> results) {
            IGlyphSet? glyphs = options.GlyphTier.HasValue && Enum.IsDefined(typeof(GlyphTier), options.GlyphTier.Value)
                ? GlyphSet.For(options.GlyphTier.Value) : null;
            StatusMarkerStyle markers = options.StatusMarkers ?? (theme is Theme concrete ? concrete.Appearance.StatusMarkers : StatusMarkerStyle.Symbols);
            if (glyphs != null) { glyphs = StyledGlyphSet.For(glyphs, BoxStyle.Rounded, markers); }
            for (int i = 0; i < Statuses.Length; i++) {
                for (int j = i + 1; j < Statuses.Length; j++) {
                    string? a = glyphs?.StatusMarker(StatusKinds[i]), b = glyphs?.StatusMarker(StatusKinds[j]);
                    bool known = !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b);
                    // Check both the wider marker and one-cell symbol used by widgets. Shared bold, for example,
                    // cannot distinguish a pair; a differing underline can.
                    bool decoration = theme.Resolve(Statuses[i], ColorDepth.TrueColor).Decoration != theme.Resolve(Statuses[j], ColorDepth.TrueColor).Decoration;
                    bool distinct = known && glyphs != null && (decoration || (a != b && glyphs!.StatusSymbol(StatusKinds[i]) != glyphs.StatusSymbol(StatusKinds[j])));
                    results.Add(Result(ThemeCheckKind.Markers, new[] { Statuses[i], Statuses[j] }, known ? (distinct ? 1 : 0) : (double?)null, 1,
                        ThemeCheckText.Pair(new[] { Statuses[i], Statuses[j] }) + " markers are different",
                        ThemeCheckText.Pair(new[] { Statuses[i], Statuses[j] }) + " markers and decoration are the same", "The rendered markers can't be checked"));
                }
            }
        }

        private static void CheckFill(ITheme theme, Color page, List<ThemeCheckResult> results) {
            Style critical = theme.Resolve(ThemeToken.Critical, ColorDepth.TrueColor);
            Color fill = FillColour(critical, page);
            if (critical.BackgroundColor.IsDefault && (critical.Decoration & TextDecoration.Reverse) == 0) {
                results.Add(Result(ThemeCheckKind.Fill, new[] { ThemeToken.Critical }, 1, 1,
                    "Critical is not filled", "", ""));
                return;
            }
            bool known = IsRgb(fill) && IsRgb(page);
            bool matches = !fill.IsDefault && fill == page;
            foreach (ThemeToken token in TextTokens.Concat(new[] { ThemeToken.Title, ThemeToken.Header, ThemeToken.Highlight, ThemeToken.Strong })) {
                if (token == ThemeToken.Critical) { continue; }
                Color background = FillColour(theme.Resolve(token, ColorDepth.TrueColor), page);
                known &= IsRgb(background);
                matches |= !fill.IsDefault && fill == background;
            }
            // Critical may deliberately share Fail's hue: its distinguishing cue is a filled background.
            // Equality is already a proven failure (also for identical palette indexes); another unknown comparison
            // cannot hide it from Strict callers.
            results.Add(Result(ThemeCheckKind.Fill, new[] { ThemeToken.Critical }, matches ? 0 : known ? 1 : (double?)null, 1,
                "Critical is filled, unlike ordinary text", "Critical is filled, but its fill matches ordinary text", "Critical's fill can't be checked without known background colours"));
        }
    }
}
