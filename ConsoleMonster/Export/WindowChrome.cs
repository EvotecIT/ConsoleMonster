using System;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Export {
    /// <summary>Shared geometry and vector drawing for static frames, animated SVG and player chrome.</summary>
    internal static partial class WindowChrome {
        internal const double Inset = 8;
        internal static double BarHeight(WindowChromeStyle style) => style switch {
            WindowChromeStyle.MacOS => 28, WindowChromeStyle.Windows => 32, WindowChromeStyle.Linux => 36, _ => 0
        };
        internal static double MinimumWidth(WindowChromeStyle style) => style switch {
            WindowChromeStyle.MacOS => 76, WindowChromeStyle.Windows => 272, WindowChromeStyle.Linux => 96, _ => 0
        };
        internal static double Radius(WindowChromeStyle style) => style == WindowChromeStyle.Windows ? 8 : style == WindowChromeStyle.Linux ? 12 : 10;
        // A fitting budget only: the renderer retains each title's natural glyph advances.
        private const double TitleAdvance = 8;
        private static double TabWidth(double canvasWidth) => Math.Min(300, Math.Max(72, Math.Max(96, canvasWidth - 230)));

        internal static void AppendTokens(StringBuilder css, string scope, ExportOptions options, bool light) {
            ITheme theme = light ? options.LightTheme ?? Theme.Light : options.DarkTheme ?? Theme.Dark;
            string surface = ExportPalette.ToCss(theme.Resolve(ThemeToken.Surface, ColorDepth.TrueColor).BackgroundColor) ?? (light ? "#e5e7eb" : "#1f2a3c");
            string muted = ExportPalette.ToCss(theme.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Color) ?? (light ? "#6b7280" : "#8b8f9a");
            string title = options.EffectiveChrome == WindowChromeStyle.None
                ? ExportPalette.ToCss(theme.Resolve(ThemeToken.Surface, ColorDepth.TrueColor).Color) ?? (light ? ExportPalette.Light.Foreground : ExportPalette.Dark.Foreground)
                : options.EffectiveChrome == WindowChromeStyle.Windows ? (light ? ExportPalette.Light.Foreground : ExportPalette.Dark.Foreground) : muted;
            string border = ExportPalette.ToCss(theme.Resolve(ThemeToken.Border, ColorDepth.TrueColor).Color) ?? (light ? "#8a8a94" : "#4b5060");
            string accent = ExportPalette.ToCss(theme.Resolve(ThemeToken.Accent, ColorDepth.TrueColor).Color) ?? (light ? "#2563eb" : "#60a5fa");
            string tab = ExportPalette.ToCss(theme.Resolve(ThemeToken.Text, ColorDepth.TrueColor).BackgroundColor) ?? (light ? ExportPalette.Light.Background : ExportPalette.Dark.Background);
            css.Append(scope).Append("{--cm-chrome:").Append(surface).Append(";--cm-chrome-fg:").Append(muted).Append(";--cm-chrome-title:").Append(title).Append(";--cm-tab:").Append(tab);
            if (options.EffectiveChrome != WindowChromeStyle.None) { css.Append(";--cm-border:").Append(border).Append(";--cm-accent:").Append(accent); }
            css.Append(";--cm-desk:").Append(light ? "#c9c9cf" : "#3a3a3f").Append(";--cm-close:").Append(light ? "#ff5f57" : "#ce534c")
                .Append(";--cm-minimise:").Append(light ? "#febc2e" : "#c59a2a").Append(";--cm-zoom:").Append(light ? "#28c840" : "#26a443").Append("}\n");
        }

    }
}
