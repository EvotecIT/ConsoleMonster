using System.Collections.Generic;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        /// <summary>Follows the terminal's sixteen palette colours at every colour depth, including true colour.</summary>
        public static Theme Terminal { get; } = BuildTerminal();

        private static Theme BuildTerminal() {
            ThemeColor P(int index) => ThemeColor.PaletteIndex(index);
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in AllTokens()) { styles[token] = new ThemeStyle(); }
            foreach (ThemeToken token in new[] { ThemeToken.Title, ThemeToken.Header, ThemeToken.Strong }) { styles[token] = new ThemeStyle(null, null, TextDecoration.Bold); }
            foreach (ThemeToken token in new[] { ThemeToken.Muted, ThemeToken.Border, ThemeToken.Skip, ThemeToken.Pending, ThemeToken.Low }) { styles[token] = new ThemeStyle(P(8)); }
            styles[ThemeToken.Subtle] = new ThemeStyle(P(8), null, TextDecoration.Dim);
            int[] indexes = { 4, 5, 2, 1, 3, 6, 6 };
            ThemeToken[] tokens = { ThemeToken.Accent, ThemeToken.Accent2, ThemeToken.Pass, ThemeToken.Fail, ThemeToken.Warn, ThemeToken.Info, ThemeToken.Running };
            for (int i = 0; i < tokens.Length; i++) { styles[tokens[i]] = new ThemeStyle(P(indexes[i])); }
            styles[ThemeToken.Highlight] = styles[ThemeToken.Focus] = new ThemeStyle(null, P(4));
            styles[ThemeToken.Surface] = new ThemeStyle(null, P(0));
            styles[ThemeToken.SurfaceAccent] = new ThemeStyle(P(4), P(0));
            int[] series = { 4, 3, 5, 6, 2, 1 };
            for (int i = 0; i < series.Length; i++) { styles[ThemeToken.Series1 + i] = new ThemeStyle(P(series[i])); }
            styles[ThemeToken.Critical] = new ThemeStyle(P(0), P(1));
            styles[ThemeToken.Gap] = new ThemeStyle(P(0));
            return new Theme("Terminal", styles) { FollowsTerminal = true };
        }
    }
}
