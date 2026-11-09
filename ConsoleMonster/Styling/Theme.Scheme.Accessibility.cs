using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        private static Theme AccessibleScheme(Theme theme) {
            TerminalScheme scheme = theme.Scheme!;
            Color[] trio = SchemeTrio(scheme);
            Color[] chosen = AdjustTrio(trio, scheme);
            HashSet<ThemeToken> adjusted = new HashSet<ThemeToken>();
            ThemeToken[] statuses = { ThemeToken.Pass, ThemeToken.Warn, ThemeToken.Fail };
            for (int i = 0; i < statuses.Length; i++) {
                if (chosen[i] != trio[i]) { adjusted.Add(statuses[i]); }
                theme = theme.With(statuses[i], new ThemeStyle(SchemeRgb(chosen[i]), null,
                    statuses[i] == ThemeToken.Fail ? TextDecoration.Underline : TextDecoration.None));
            }
            // Check every actual text/background pair, including muted severity labels such as Gap.
            foreach (ThemeToken token in AllTokens()) {
                if (token == ThemeToken.Subtle || token == ThemeToken.Critical || statuses.Contains(token)) { continue; }
                ThemeStyle style = theme.Get(token);
                if (!style.Color.HasValue) { continue; }
                Color original = style.Color.Value.TrueColor, background = style.BackgroundColor?.TrueColor ?? scheme.Background;
                if (ThemeCheck.Contrast(original, background) >= 4.5) { continue; }
                List<Color> candidates = SchemeLightness.Candidates(original, background, 4.5);
                if (candidates.Count == 0) { continue; }
                theme = theme.With(token, new ThemeStyle(SchemeRgb(candidates[0]), style.BackgroundColor, style.Decoration));
                adjusted.Add(token);
            }
            ThemeStyle critical = theme.Get(ThemeToken.Critical);
            Color fill = critical.BackgroundColor!.Value.TrueColor;
            if (ThemeCheck.Contrast(scheme.Background, fill) < 4.5) {
                List<Color> fills = SchemeLightness.Candidates(fill, scheme.Background, 4.5);
                if (fills.Count > 0) {
                    theme = theme.With(ThemeToken.Critical, new ThemeStyle(critical.Color, SchemeRgb(fills[0]), critical.Decoration));
                    adjusted.Add(ThemeToken.Critical);
                }
            }
            theme.AdjustedTokens = Array.AsReadOnly(AllTokens().Where(adjusted.Contains).ToArray());
            theme.ClearSchemeOverrides();
            return theme;
        }

        // Step 1 maximises the worst pair under all ThemeCheck vision conditions, keeping 3:1 against the page.
        private static Color[] SchemeTrio(TerminalScheme scheme) {
            Color[] best = { scheme.Palette[4], scheme.Palette[3], scheme.Palette[1] };
            double bestScore = -1;
            for (int constrained = 1; constrained >= 0; constrained--) {
                foreach (Color a in StatusColors(scheme, 0, false)) {
                    foreach (Color b in StatusColors(scheme, 1, false)) {
                        foreach (Color c in StatusColors(scheme, 2, false)) {
                            if (constrained == 1 && (ThemeCheck.Contrast(a, scheme.Background) < 3
                                || ThemeCheck.Contrast(b, scheme.Background) < 3 || ThemeCheck.Contrast(c, scheme.Background) < 3)) { continue; }
                            double score = TrioDistance(a, b, c);
                            if (score > bestScore) { bestScore = score; best = new[] { a, b, c }; }
                        }
                    }
                }
                if (bestScore >= 0) { break; }
            }
            return best;
        }

        private static Color[] AdjustTrio(Color[] original, TerminalScheme scheme) {
            Color background = scheme.Background;
            if (original.All(color => ThemeCheck.Contrast(color, background) >= 4.5) && TrioDistance(original[0], original[1], original[2]) >= 25) { return original; }
            List<Color>[] candidates = original.Select(color => SchemeLightness.Candidates(color, background, 4.5)).ToArray();
            if (FindTrio(candidates, original, false, out Color[] chosen)) { return chosen; }
            // A hue that loses its chroma at the required lightness needs another published colour in the same family.
            candidates = Enumerable.Range(0, 3).Select(role => StatusColors(scheme, role, true)
                .SelectMany(color => SchemeLightness.Candidates(color, background, 4.5)).Distinct()
                .OrderByDescending(color => new SchemeLightness(color).Chroma).ToList()).ToArray();
            if (FindTrio(candidates, original, true, out Color[] expanded)) { return expanded; }
            // Importing never rejects a failed check, even when a palette cannot meet all three bars.
            return chosen;
        }

        private static bool FindTrio(List<Color>[] candidates, Color[] original, bool mostChroma, out Color[] best) {
            best = candidates.Select((list, i) => list.Count == 0 ? original[i] : list[0]).ToArray();
            if (candidates.Any(list => list.Count == 0)) { return false; }
            double bestCost = double.MaxValue;
            double[] initial = original.Select(color => new SchemeLightness(color).Lightness).ToArray();
            double Cost(Color color, int role) => mostChroma ? -new SchemeLightness(color).Chroma : Math.Abs(new SchemeLightness(color).Lightness - initial[role]);
            double wmin = candidates[1].Min(color => Cost(color, 1)), fmin = candidates[2].Min(color => Cost(color, 2));
            foreach (Color p in candidates[0]) {
                double pcost = Cost(p, 0);
                if (pcost + wmin + fmin >= bestCost) { continue; }
                foreach (Color w in candidates[1]) {
                    double cost = pcost + Cost(w, 1);
                    if (cost + fmin >= bestCost) { continue; }
                    double pw = ThemeCheck.MinimumStatusDistance(p, w);
                    if (pw < 25) { continue; }
                    foreach (Color f in candidates[2]) {
                        double total = cost + Cost(f, 2);
                        if (total >= bestCost) { continue; }
                        double distance = Math.Min(pw, Math.Min(ThemeCheck.MinimumStatusDistance(p, f), ThemeCheck.MinimumStatusDistance(w, f)));
                        if (distance >= 25) { bestCost = total; best = new[] { p, w, f }; }
                    }
                }
            }
            return bestCost != double.MaxValue;
        }

        private static Color[] StatusColors(TerminalScheme scheme, int role, bool expanded) {
            int[] bases = role == 0 ? new[] { 4, 6 } : role == 1 ? new[] { 3 } : new[] { 1, 5 };
            List<Color> colors = bases.SelectMany(index => new[] { scheme.Palette[index], scheme.Palette[index + 8] }
                .Where(color => SchemeLightness.IsRoleCandidate(scheme.Palette[index], color))).Distinct().ToList();
            if (expanded) {
                colors.AddRange(scheme.Palette.Where(color => {
                    SchemeLightness lch = new SchemeLightness(color);
                    return lch.Chroma >= .05 && (role == 1 ? lch.Hue >= 30 && lch.Hue <= 120
                        : HueBetween(lch.Hue, new SchemeLightness(scheme.Palette[bases[0]]).Hue, new SchemeLightness(scheme.Palette[bases[1]]).Hue));
                }));
            }
            if (role == 2) {
                double red = new SchemeLightness(scheme.Palette[1]).Hue, magenta = new SchemeLightness(scheme.Palette[5]).Hue;
                colors = colors.Where(color => HueBetween(new SchemeLightness(color).Hue, red, magenta, 10)).ToList();
            }
            return colors.Count == 0 ? new[] { scheme.Palette[bases[0]] } : colors.Distinct().ToArray();
        }

        private static bool HueBetween(double hue, double a, double b, double padding = 0) {
            double span = (b - a + 360) % 360;
            if (span > 180) { return HueBetween(hue, b, a, padding); }
            return (hue - a + 360) % 360 <= span + padding || (a - hue + 360) % 360 <= padding;
        }
        private static double TrioDistance(Color p, Color w, Color f) => Math.Min(ThemeCheck.MinimumStatusDistance(p, w),
            Math.Min(ThemeCheck.MinimumStatusDistance(p, f), ThemeCheck.MinimumStatusDistance(w, f)));
        private static ThemeColor SchemeRgb(Color color) => new ThemeColor(color, color.Downsample(ColorDepth.Ansi16).Index);
    }
}
