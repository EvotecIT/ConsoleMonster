using System.Collections.Generic;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        /// <summary>The default theme, tuned for dark terminal backgrounds (Windows Terminal, conhost, VS Code dark).</summary>
        public static Theme Dark { get; } = BuildDark();

        /// <summary>A theme for light terminal backgrounds.</summary>
        public static Theme Light { get; } = BuildLight();

        /// <summary>A high-contrast theme: saturated primaries, bold status, white borders.</summary>
        public static Theme HighContrast { get; } = BuildHighContrast();

        /// <summary>
        /// A dark-background preset with blue/orange status contrast, explicit low-colour fallbacks and bold/underlined
        /// emphasis. Widgets retain their status labels and glyphs in every colour tier.
        /// </summary>
        /// <remarks>Terminal palettes and individual colour perception vary; this is a starting point, not an accessibility certification.</remarks>
        public static Theme ColorBlindSafe { get; } = BuildColorBlindSafe();

        // Titles, headers and highlights use the terminal's own foreground (bold), so the dark theme stays readable when
        // it is used on a light background that could not be detected (for example inside Windows Terminal).
        private static Theme BuildDark() {
            Dictionary<ThemeToken, ThemeStyle> s = new Dictionary<ThemeToken, ThemeStyle> {
                [ThemeToken.Text] = new ThemeStyle(null),
                [ThemeToken.Muted] = new ThemeStyle(ThemeColor.Hex("#8b8f9a", 8, 245)),
                [ThemeToken.Subtle] = new ThemeStyle(ThemeColor.Hex("#3a3d45", 8, 237)),
                [ThemeToken.Accent] = new ThemeStyle(ThemeColor.Hex("#60a5fa", 14, 75)),
                [ThemeToken.Accent2] = new ThemeStyle(ThemeColor.Hex("#c084fc", 13, 141)),
                [ThemeToken.Pass] = new ThemeStyle(ThemeColor.Hex("#4ade80", 10, 78)),
                [ThemeToken.Fail] = new ThemeStyle(ThemeColor.Hex("#f87171", 9, 203)),
                [ThemeToken.Warn] = new ThemeStyle(ThemeColor.Hex("#fbbf24", 11, 214)),
                [ThemeToken.Info] = new ThemeStyle(ThemeColor.Hex("#38bdf8", 14, 39)),
                [ThemeToken.Skip] = new ThemeStyle(ThemeColor.Hex("#a1a1aa", 7, 248)),
                [ThemeToken.Running] = new ThemeStyle(ThemeColor.Hex("#60a5fa", 12, 75)),
                [ThemeToken.Pending] = new ThemeStyle(ThemeColor.Hex("#71717a", 8, 243)),
                [ThemeToken.Border] = new ThemeStyle(ThemeColor.Hex("#4b5060", 8, 240)),
                [ThemeToken.Title] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Header] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Highlight] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Surface] = new ThemeStyle(ThemeColor.Hex("#e2e8f0", 15, 254), ThemeColor.Hex("#1f2a3c", 4, 236)),
                [ThemeToken.SurfaceAccent] = new ThemeStyle(ThemeColor.Hex("#93c5fd", 14, 117), ThemeColor.Hex("#1f2a3c", 4, 236), TextDecoration.Bold),
                [ThemeToken.Series1] = new ThemeStyle(ThemeColor.Hex("#60a5fa", 12, 75)),
                [ThemeToken.Series2] = new ThemeStyle(ThemeColor.Hex("#4ade80", 10, 78)),
                [ThemeToken.Series3] = new ThemeStyle(ThemeColor.Hex("#fbbf24", 11, 214)),
                [ThemeToken.Series4] = new ThemeStyle(ThemeColor.Hex("#f87171", 9, 203)),
                [ThemeToken.Series5] = new ThemeStyle(ThemeColor.Hex("#c084fc", 13, 141)),
                [ThemeToken.Series6] = new ThemeStyle(ThemeColor.Hex("#2dd4bf", 14, 43)),
                [ThemeToken.Strong] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Low] = new ThemeStyle(ThemeColor.Hex("#cdbb7c", 11, 179)),
                [ThemeToken.Critical] = new ThemeStyle(ThemeColor.Hex("#ff5a8f", 9, 204), null, TextDecoration.Bold),
                [ThemeToken.Gap] = new ThemeStyle(ThemeColor.Hex("#b893f5", 12, 141)),
                [ThemeToken.Focus] = new ThemeStyle(ThemeColor.Hex("#f1f5f9", 15, 255), ThemeColor.Hex("#243b5e", 4, 237))
            };
            return new Theme("Dark", s);
        }

        private static Theme BuildLight() {
            Dictionary<ThemeToken, ThemeStyle> s = new Dictionary<ThemeToken, ThemeStyle> {
                [ThemeToken.Text] = new ThemeStyle(null),
                [ThemeToken.Muted] = new ThemeStyle(ThemeColor.Hex("#6b7280", 8, 243)),
                [ThemeToken.Subtle] = new ThemeStyle(ThemeColor.Hex("#d4d4d8", 7, 252)),
                [ThemeToken.Accent] = new ThemeStyle(ThemeColor.Hex("#2563eb", 4, 26)),
                [ThemeToken.Accent2] = new ThemeStyle(ThemeColor.Hex("#7c3aed", 5, 92)),
                [ThemeToken.Pass] = new ThemeStyle(ThemeColor.Hex("#15803d", 2, 28)),
                [ThemeToken.Fail] = new ThemeStyle(ThemeColor.Hex("#b91c1c", 1, 124)),
                [ThemeToken.Warn] = new ThemeStyle(ThemeColor.Hex("#b45309", 3, 130)),
                [ThemeToken.Info] = new ThemeStyle(ThemeColor.Hex("#0369a1", 6, 25)),
                [ThemeToken.Skip] = new ThemeStyle(ThemeColor.Hex("#71717a", 8, 243)),
                [ThemeToken.Running] = new ThemeStyle(ThemeColor.Hex("#2563eb", 4, 26)),
                [ThemeToken.Pending] = new ThemeStyle(ThemeColor.Hex("#858590", 8, 244)),
                [ThemeToken.Border] = new ThemeStyle(ThemeColor.Hex("#8a8a94", 8, 245)),
                [ThemeToken.Title] = new ThemeStyle(ThemeColor.Hex("#111827", 0, 233), null, TextDecoration.Bold),
                [ThemeToken.Header] = new ThemeStyle(ThemeColor.Hex("#374151", 0, 237), null, TextDecoration.Bold),
                [ThemeToken.Highlight] = new ThemeStyle(ThemeColor.Hex("#111827", 0, 233), null, TextDecoration.Bold),
                [ThemeToken.Surface] = new ThemeStyle(ThemeColor.Hex("#111827", 0, 233), ThemeColor.Hex("#e5e7eb", 7, 254)),
                [ThemeToken.SurfaceAccent] = new ThemeStyle(ThemeColor.Hex("#1d4ed8", 4, 26), ThemeColor.Hex("#e5e7eb", 7, 254), TextDecoration.Bold),
                [ThemeToken.Series1] = new ThemeStyle(ThemeColor.Hex("#2563eb", 4, 26)),
                [ThemeToken.Series2] = new ThemeStyle(ThemeColor.Hex("#16a34a", 2, 34)),
                [ThemeToken.Series3] = new ThemeStyle(ThemeColor.Hex("#d97706", 3, 172)),
                [ThemeToken.Series4] = new ThemeStyle(ThemeColor.Hex("#dc2626", 1, 160)),
                [ThemeToken.Series5] = new ThemeStyle(ThemeColor.Hex("#7c3aed", 5, 92)),
                [ThemeToken.Series6] = new ThemeStyle(ThemeColor.Hex("#0d9488", 6, 30)),
                [ThemeToken.Strong] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Low] = new ThemeStyle(ThemeColor.Hex("#76641a", 3, 94)),
                [ThemeToken.Critical] = new ThemeStyle(ThemeColor.Hex("#ad0f56", 5, 125), null, TextDecoration.Bold),
                [ThemeToken.Gap] = new ThemeStyle(ThemeColor.Hex("#7339bd", 13, 92)),
                [ThemeToken.Focus] = new ThemeStyle(ThemeColor.Hex("#0f172a", 0, 233), ThemeColor.Hex("#d6e4fa", 7, 189))
            };
            return new Theme("Light", s);
        }

        private static Theme BuildHighContrast() {
            Dictionary<ThemeToken, ThemeStyle> s = new Dictionary<ThemeToken, ThemeStyle> {
                [ThemeToken.Text] = new ThemeStyle(ThemeColor.Hex("#ffffff", 15, 231)),
                [ThemeToken.Muted] = new ThemeStyle(ThemeColor.Hex("#d0d0d0", 7, 252)),
                [ThemeToken.Subtle] = new ThemeStyle(ThemeColor.Hex("#808080", 8, 244)),
                [ThemeToken.Accent] = new ThemeStyle(ThemeColor.Hex("#00ffff", 14, 51), null, TextDecoration.Bold),
                [ThemeToken.Accent2] = new ThemeStyle(ThemeColor.Hex("#ff00ff", 13, 201)),
                [ThemeToken.Pass] = new ThemeStyle(ThemeColor.Hex("#00ff00", 10, 46), null, TextDecoration.Bold),
                [ThemeToken.Fail] = new ThemeStyle(ThemeColor.Hex("#ff3030", 9, 196), null, TextDecoration.Bold),
                [ThemeToken.Warn] = new ThemeStyle(ThemeColor.Hex("#ffff00", 11, 226), null, TextDecoration.Bold),
                [ThemeToken.Info] = new ThemeStyle(ThemeColor.Hex("#00ffff", 14, 51)),
                [ThemeToken.Skip] = new ThemeStyle(ThemeColor.Hex("#d0d0d0", 7, 252)),
                [ThemeToken.Running] = new ThemeStyle(ThemeColor.Hex("#00ffff", 14, 51)),
                [ThemeToken.Pending] = new ThemeStyle(ThemeColor.Hex("#d0d0d0", 7, 252)),
                [ThemeToken.Border] = new ThemeStyle(ThemeColor.Hex("#ffffff", 15, 231)),
                [ThemeToken.Title] = new ThemeStyle(ThemeColor.Hex("#ffff00", 11, 226), null, TextDecoration.Bold),
                [ThemeToken.Header] = new ThemeStyle(ThemeColor.Hex("#ffffff", 15, 231), null, TextDecoration.Bold | TextDecoration.Underline),
                [ThemeToken.Highlight] = new ThemeStyle(ThemeColor.Hex("#ffffff", 15, 231), null, TextDecoration.Bold),
                [ThemeToken.Surface] = new ThemeStyle(ThemeColor.Hex("#000000", 0, 16), ThemeColor.Hex("#ffffff", 15, 231)),
                [ThemeToken.SurfaceAccent] = new ThemeStyle(ThemeColor.Hex("#000000", 0, 16), ThemeColor.Hex("#ffffff", 15, 231), TextDecoration.Bold),
                [ThemeToken.Series1] = new ThemeStyle(ThemeColor.Hex("#00ffff", 14, 51)),
                [ThemeToken.Series2] = new ThemeStyle(ThemeColor.Hex("#00ff00", 10, 46)),
                [ThemeToken.Series3] = new ThemeStyle(ThemeColor.Hex("#ffff00", 11, 226)),
                [ThemeToken.Series4] = new ThemeStyle(ThemeColor.Hex("#ff3030", 9, 196)),
                [ThemeToken.Series5] = new ThemeStyle(ThemeColor.Hex("#ff00ff", 13, 201)),
                [ThemeToken.Series6] = new ThemeStyle(ThemeColor.Hex("#ffffff", 15, 231)),
                [ThemeToken.Strong] = new ThemeStyle(null, null, TextDecoration.Bold),
                [ThemeToken.Low] = new ThemeStyle(ThemeColor.Hex("#cdbb7c", 11, 179)),
                [ThemeToken.Critical] = new ThemeStyle(ThemeColor.Hex("#ff5a8f", 9, 204), null, TextDecoration.Bold),
                [ThemeToken.Gap] = new ThemeStyle(ThemeColor.Hex("#b893f5", 12, 141)),
                [ThemeToken.Focus] = new ThemeStyle(ThemeColor.Hex("#000000", 0, 16), ThemeColor.Hex("#00ffff", 14, 51), TextDecoration.Bold)
            };
            return new Theme("HighContrast", s);
        }
    }
}
