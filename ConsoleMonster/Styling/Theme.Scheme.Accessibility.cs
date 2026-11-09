using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        private static Theme AccessibleScheme(Theme theme) {
            TerminalScheme scheme = theme.Scheme!;
            Color[] trio = SchemeTrio(scheme);
            Color[] chosen = AdjustTrio(trio, scheme.Background);
            HashSet<ThemeToken> adjusted = new HashSet<ThemeToken>();
            ThemeToken[] statuses = { ThemeToken.Pass, ThemeToken.Warn, ThemeToken.Fail };
            for (int i = 0; i < statuses.Length; i++) {
                if (chosen[i] != trio[i]) { adjusted.Add(statuses[i]); }
                theme = theme.With(statuses[i], new ThemeStyle(SchemeRgb(chosen[i]), null,
                    statuses[i] == ThemeToken.Fail ? TextDecoration.Underline : TextDecoration.None));
            }
            // Check every actual text/background pair. Gap is also a severity label, despite its background-coloured faithful mapping.
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
                foreach (int p in new[] { 4, 12, 6, 14 }) {
                    foreach (int w in new[] { 3, 11 }) {
                        foreach (int f in new[] { 1, 9, 5, 13 }) {
                            Color a = scheme.Palette[p], b = scheme.Palette[w], c = scheme.Palette[f];
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

        private static Color[] AdjustTrio(Color[] original, Color background) {
            if (original.All(color => ThemeCheck.Contrast(color, background) >= 4.5) && TrioDistance(original[0], original[1], original[2]) >= 25) { return original; }
            List<Color>[] candidates = original.Select(color => SchemeLightness.Candidates(color, background, 4.5)).ToArray();
            if (candidates.Any(list => list.Count == 0)) { return original; }
            Color[] best = candidates.Select(list => list[0]).ToArray();
            double bestCost = double.MaxValue, bestDistance = -1;
            double[] initial = original.Select(color => new SchemeLightness(color).Lightness).ToArray();
            foreach (Color p in candidates[0]) {
                double pcost = Math.Abs(new SchemeLightness(p).Lightness - initial[0]);
                if (pcost >= bestCost) { continue; }
                foreach (Color w in candidates[1]) {
                    double cost = pcost + Math.Abs(new SchemeLightness(w).Lightness - initial[1]);
                    if (cost >= bestCost) { continue; }
                    double pw = ThemeCheck.MinimumStatusDistance(p, w);
                    foreach (Color f in candidates[2]) {
                        double total = cost + Math.Abs(new SchemeLightness(f).Lightness - initial[2]);
                        if (total >= bestCost) { continue; }
                        double distance = Math.Min(pw, Math.Min(ThemeCheck.MinimumStatusDistance(p, f), ThemeCheck.MinimumStatusDistance(w, f)));
                        if (distance >= 25) { bestCost = total; best = new[] { p, w, f }; }
                        else if (bestCost == double.MaxValue && distance > bestDistance) { bestDistance = distance; best = new[] { p, w, f }; }
                    }
                }
            }
            // Imported palettes may make the bars impossible. Keep the best measured result; importing never rejects a failed check.
            return best;
        }
        private static double TrioDistance(Color p, Color w, Color f) => Math.Min(ThemeCheck.MinimumStatusDistance(p, w),
            Math.Min(ThemeCheck.MinimumStatusDistance(p, f), ThemeCheck.MinimumStatusDistance(w, f)));
        private static ThemeColor SchemeRgb(Color color) => new ThemeColor(color, color.Downsample(ColorDepth.Ansi16).Index);
    }
}
