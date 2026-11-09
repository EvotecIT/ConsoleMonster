using System.Collections.Generic;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        internal Theme ResolvePalette(TerminalScheme scheme) {
            ThemeColor? Resolve(ThemeColor? color) {
                if (!color.HasValue) { return null; }
                Color resolved = color.Value.TrueColor;
                if ((resolved.Kind == ColorKind.Ansi16 || resolved.Kind == ColorKind.Ansi256) && resolved.Index < 16) { resolved = scheme.Palette[resolved.Index]; }
                return new ThemeColor(resolved, color.Value.Ansi16, color.Value.Ansi256);
            }
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in AllTokens()) {
                ThemeStyle style = Get(token);
                styles[token] = new ThemeStyle(Resolve(style.Color) ?? SchemeRgb(scheme.Foreground), Resolve(style.BackgroundColor), style.Decoration);
            }
            ThemeBackground background = Theme.FromScheme(scheme).Appearance.Background;
            ThemeAppearance appearance = new ThemeAppearance(Appearance.Density, Appearance.Border, Appearance.StatusMarkers, background);
            return new Theme(Name, styles, appearance) { Scheme = scheme, IsAccessible = IsAccessible, AdjustedTokens = AdjustedTokens }.WithAllSchemeOverrides();
        }

        internal Theme ForExport() {
            if (!FollowsTerminal) { return this; }
            TerminalScheme? scheme = global::ConsoleMonster.Terminal.Palette;
            if (scheme == null) {
                Color[] colors = new Color[16];
                for (int i = 0; i < 16; i++) { AnsiPalette.Get16(i, out byte r, out byte g, out byte b); colors[i] = Color.FromRgb(r, g, b); }
                bool light = Output.TerminalBackground.TryDetect(out Output.TerminalBackgroundInfo known)
                    ? known.IsLight : Appearance.Background == ThemeBackground.Light;
                scheme = new TerminalScheme("Terminal (fallback)", colors, Color.FromHex(light ? "#24292f" : "#cccccc"), Color.FromHex(light ? "#ffffff" : "#0c0c0c"));
            }
            return ResolvePalette(scheme);
        }
    }
}
