using System.Collections.Generic;
namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        internal static Theme ConsoleViewDark { get; } = BuildConsoleViewDark();
        internal static Theme ConsoleViewLight { get; } = BuildConsoleViewLight();
        private static Theme BuildConsoleViewDark() {
            Dictionary<ThemeToken, ThemeStyle> s = new Dictionary<ThemeToken, ThemeStyle> {
                [ThemeToken.Text] = new ThemeStyle(null),
                [ThemeToken.Muted] = new ThemeStyle(ThemeColor.Hex("#9198a4", 8, 246)),
                [ThemeToken.Subtle] = new ThemeStyle(ThemeColor.Hex("#565d6b", 8, 240)),
                [ThemeToken.Accent] = new ThemeStyle(ThemeColor.Hex("#82aaff", 15, 111), null, TextDecoration.Bold),
                [ThemeToken.Accent2] = new ThemeStyle(ThemeColor.Hex("#c084fc", 13, 141)),
                [ThemeToken.Pass] = new ThemeStyle(ThemeColor.Hex("#55c98c", 10, 78)),
                [ThemeToken.Fail] = new ThemeStyle(ThemeColor.Hex("#f4675f", 9, 203)),
                [ThemeToken.Warn] = new ThemeStyle(ThemeColor.Hex("#f2b640", 11, 214)),
                [ThemeToken.Info] = new ThemeStyle(ThemeColor.Hex("#4fc1d3", 14, 74)),
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
            return new Theme("ConsoleViewDark", s);
        }
        private static Theme BuildConsoleViewLight() {
            Dictionary<ThemeToken, ThemeStyle> s = new Dictionary<ThemeToken, ThemeStyle> {
                [ThemeToken.Text] = new ThemeStyle(null),
                [ThemeToken.Muted] = new ThemeStyle(ThemeColor.Hex("#646b77", 8, 242)),
                [ThemeToken.Subtle] = new ThemeStyle(ThemeColor.Hex("#aeb4be", 7, 249)),
                [ThemeToken.Accent] = new ThemeStyle(ThemeColor.Hex("#2c5cc5", 4, 26), null, TextDecoration.Bold),
                [ThemeToken.Accent2] = new ThemeStyle(ThemeColor.Hex("#7c3aed", 5, 92)),
                [ThemeToken.Pass] = new ThemeStyle(ThemeColor.Hex("#1b7a42", 2, 29)),
                [ThemeToken.Fail] = new ThemeStyle(ThemeColor.Hex("#c0302a", 1, 160)),
                [ThemeToken.Warn] = new ThemeStyle(ThemeColor.Hex("#955f00", 3, 130)),
                [ThemeToken.Info] = new ThemeStyle(ThemeColor.Hex("#08778a", 6, 30)),
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
            return new Theme("ConsoleViewLight", s);
        }
    }
}
