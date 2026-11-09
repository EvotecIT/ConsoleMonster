using System;
using System.Collections.Generic;
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
            Color page = options.Background ?? (theme is Theme concrete
                ? Color.FromHex(concrete.Appearance.Background == ThemeBackground.Light ? "#ffffff" : "#0c0c0c") : Color.Default);
            List<ThemeCheckResult> results = new List<ThemeCheckResult>();
            foreach (ThemeToken token in TextTokens) {
                Style style = theme.Resolve(token, ColorDepth.TrueColor);
                Color background = style.BackgroundColor.IsDefault ? page : style.BackgroundColor;
                double? ratio = IsRgb(style.Color) && IsRgb(background) ? Contrast(style.Color, background) : (double?)null;
                results.Add(Result(ThemeCheckKind.Contrast, new[] { token }, ratio, 4.5,
                    "Readable text.", "Text contrast is too low.", "Text RGB is unknown."));
            }
            for (int vision = 0; vision < Simulations.Length; vision++) {
                for (int i = 0; i < Statuses.Length; i++) {
                    for (int j = i + 1; j < Statuses.Length; j++) {
                        Color a = TextColour(theme.Resolve(Statuses[i], ColorDepth.TrueColor), page);
                        Color b = TextColour(theme.Resolve(Statuses[j], ColorDepth.TrueColor), page);
                        double? distance = IsRgb(a) && IsRgb(b) ? Distance(Lab(a, Simulations[vision]), Lab(b, Simulations[vision])) : (double?)null;
                        results.Add(Result(ThemeCheckKind.ColourVision, new[] { Statuses[i], Statuses[j] }, distance, 25,
                            "Apart under " + VisionNames[vision] + ".", "Too close under " + VisionNames[vision] + ".", "RGB unknown: " + VisionNames[vision] + "."));
                    }
                }
            }
            CheckMarkers(theme, options, results);
            CheckFill(theme, page, results);
            return new ThemeCheckReport(results);
        }

        private static Color TextColour(Style style, Color page) => (style.Decoration & TextDecoration.Reverse) == 0
            ? style.Color : style.BackgroundColor.IsDefault ? page : style.BackgroundColor;

        private static Color FillColour(Style style, Color page) => (style.Decoration & TextDecoration.Reverse) != 0
            ? style.Color : style.BackgroundColor.IsDefault ? page : style.BackgroundColor;

        private static bool IsRgb(Color colour) => colour.Kind == ColorKind.Rgb;

        private static ThemeCheckResult Result(ThemeCheckKind check, ThemeToken[] tokens, double? value, double required, string pass, string fail, string unknown) =>
            new ThemeCheckResult(check, tokens, !value.HasValue ? ThemeCheckOutcome.Unknown : value.Value >= required ? ThemeCheckOutcome.Pass : ThemeCheckOutcome.Fail,
                value, required, !value.HasValue ? unknown : value.Value >= required ? pass : fail);

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
                        "Different without colour.", "Same markers and decoration.", "Renderable markers are unknown."));
                }
            }
        }

        private static void CheckFill(ITheme theme, Color page, List<ThemeCheckResult> results) {
            Style critical = theme.Resolve(ThemeToken.Critical, ColorDepth.TrueColor);
            Color fill = FillColour(critical, page);
            if (critical.BackgroundColor.IsDefault && (critical.Decoration & TextDecoration.Reverse) == 0) {
                results.Add(Result(ThemeCheckKind.Fill, new[] { ThemeToken.Critical }, 1, 1,
                    "Critical is unfilled.", "", ""));
                return;
            }
            bool known = IsRgb(fill) && IsRgb(page);
            bool distinct = fill != page;
            foreach (ThemeToken token in Statuses) {
                Color background = FillColour(theme.Resolve(token, ColorDepth.TrueColor), page);
                known &= IsRgb(background);
                distinct &= fill != background;
            }
            // Critical may deliberately share Fail's hue: its distinguishing cue is a filled background.
            results.Add(Result(ThemeCheckKind.Fill, new[] { ThemeToken.Critical }, known ? (distinct ? 1 : 0) : (double?)null, 1,
                "Critical has a distinct fill.", "Fill matches ordinary text.", "Fill RGB is unknown."));
        }
    }
}
