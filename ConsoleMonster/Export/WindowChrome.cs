using System;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Export {
    /// <summary>One geometry and vector drawing owner for static frames, animated SVG and player chrome.</summary>
    internal static class WindowChrome {
        internal static double BarHeight(WindowChromeStyle style) => style switch {
            WindowChromeStyle.MacOS => 28, WindowChromeStyle.Windows => 32, WindowChromeStyle.Linux => 36, _ => 0
        };
        internal static double MinimumWidth(WindowChromeStyle style) => style switch {
            WindowChromeStyle.MacOS => 76, WindowChromeStyle.Windows => 208, WindowChromeStyle.Linux => 96, _ => 0
        };
        private static double Left(WindowChromeStyle style) => style == WindowChromeStyle.Windows ? 40 : style == WindowChromeStyle.Linux ? 48 : 76;
        private static double Right(WindowChromeStyle style) => style == WindowChromeStyle.Windows ? 144 : style == WindowChromeStyle.Linux ? 48 : 76;
        private static double Radius(WindowChromeStyle style) => style == WindowChromeStyle.Windows ? 8 : style == WindowChromeStyle.Linux ? 12 : 10;
        // A fixed title advance makes standalone SVG fitting independent of installed fonts.
        private const double TitleAdvance = 8;

        internal static string HtmlStart(string? title, WindowChromeStyle style) =>
            "<div class=\"cm-window cm-chrome-" + style.ToString().ToLowerInvariant() + "\">" + HtmlBar(title, style);

        internal static string HtmlBar(string? title, WindowChromeStyle style) {
            if (style == WindowChromeStyle.None) { return string.Empty; }
            StringBuilder html = new StringBuilder("<div class=\"cm-titlebar\">");
            if (style == WindowChromeStyle.MacOS) {
                html.Append("<span class=\"cm-dots\" aria-hidden=\"true\"><i></i><i></i><i></i></span>");
            } else {
                if (style == WindowChromeStyle.Windows) {
                    html.Append("<span class=\"cm-tab\" aria-hidden=\"true\"></span>");
                    HtmlControl(html, "cm-left", 36, style, false);
                }
                HtmlControl(html, "cm-right", style == WindowChromeStyle.Windows ? 140 : 48, style, true);
            }
            string escaped = ExportText.Escape(title);
            html.Append("<span class=\"cm-title\" title=\"").Append(escaped).Append("\">").Append(escaped).Append("</span></div>");
            return html.ToString();
        }

        private static void HtmlControl(StringBuilder html, string cls, double width, WindowChromeStyle style, bool right) {
            html.Append("<svg class=\"").Append(cls).Append("\" xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
                .Append("\" height=\"").Append(BarHeight(style)).Append("\" viewBox=\"0 0 ").Append(width).Append(' ').Append(BarHeight(style)).Append("\" aria-hidden=\"true\">");
            if (right) { AppendRight(html, width, style); }
            else { AppendPrompt(html); }
            html.Append("</svg>");
        }

        internal static void AppendHtmlCss(StringBuilder output, WindowChromeStyle style) {
            if (style == WindowChromeStyle.None) { return; }
            StringBuilder css = new StringBuilder();
            double bar = BarHeight(style), left = Left(style), right = Right(style);
            css.Append(".cm-export .cm-window{display:inline-block;min-width:").Append(MinimumWidth(style)).Append("px;border-radius:").Append(Radius(style))
                .Append("px;overflow:hidden;box-shadow:0 10px 30px rgba(0,0,0,.35),0 0 0 1px rgba(127,127,127,.25);background:var(--cm-bg)}\n");
            css.Append(".cm-export .cm-titlebar{position:relative;container-type:inline-size;min-width:").Append(MinimumWidth(style)).Append("px;height:").Append(bar)
                .Append("px;background:var(--cm-chrome);color:var(--cm-chrome-fg)}\n");
            css.Append(".cm-export .cm-titlebar .cm-title{position:absolute;left:").Append(left).Append("px;right:").Append(right)
                .Append("px;top:0;display:block;min-width:0;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;color:var(--cm-chrome-title);text-align:").Append(style == WindowChromeStyle.Windows ? "left" : "center")
                .Append(";font:").Append(style == WindowChromeStyle.Linux ? "bold " : "").Append("13px/").Append(bar).Append("px monospace}\n");
            css.Append("@container (width < ").Append(left + right + 8).Append("px){.cm-export .cm-titlebar .cm-title{visibility:hidden}}\n");
            css.Append(".cm-export .cm-titlebar svg{position:absolute;top:0;color:var(--cm-chrome-fg)}\n.cm-export .cm-left{left:0}.cm-export .cm-right{right:0}\n");
            if (style == WindowChromeStyle.MacOS) {
                css.Append(".cm-export .cm-dots{position:absolute;left:12px;top:8px;display:flex;gap:8px;height:12px}\n.cm-export .cm-dots i{display:block;flex:none;width:12px;height:12px;border-radius:50%;background:var(--cm-close)}\n.cm-export .cm-dots i+i{background:var(--cm-minimise)}\n.cm-export .cm-dots i+i+i{background:var(--cm-zoom)}\n");
            } else if (style == WindowChromeStyle.Windows) {
                css.Append(".cm-export .cm-tab{position:absolute;left:4px;right:140px;top:4px;bottom:0;border-radius:6px 6px 0 0;background:var(--cm-tab)}\n");
            }
            string window = ".cm-export .cm-window.cm-chrome-" + style.ToString().ToLowerInvariant();
            output.Append(css.ToString().Replace(".cm-export .cm-window", window)
                .Replace(".cm-export .cm-titlebar", window + " .cm-titlebar")
                .Replace(".cm-export .cm-dots", window + " .cm-dots")
                .Replace(".cm-export .cm-left", window + " .cm-left")
                .Replace(".cm-export .cm-right", window + " .cm-right")
                .Replace(".cm-export .cm-tab", window + " .cm-tab"));
        }

        internal static void AppendTokens(StringBuilder css, string scope, ExportOptions options, bool light) {
            ITheme theme = light ? options.LightTheme ?? Theme.Light : options.DarkTheme ?? Theme.Dark;
            string surface = ExportPalette.ToCss(theme.Resolve(ThemeToken.Surface, ColorDepth.TrueColor).BackgroundColor) ?? (light ? "#e5e7eb" : "#1f2a3c");
            string muted = ExportPalette.ToCss(theme.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Color) ?? (light ? "#6b7280" : "#a1a1aa");
            string title = ExportPalette.ToCss(theme.Resolve(ThemeToken.Surface, ColorDepth.TrueColor).Color) ?? (light ? ExportPalette.Light.Foreground : ExportPalette.Dark.Foreground);
            string tab = ExportPalette.ToCss(theme.Resolve(ThemeToken.Text, ColorDepth.TrueColor).BackgroundColor) ?? (light ? ExportPalette.Light.Background : ExportPalette.Dark.Background);
            css.Append(scope).Append("{--cm-chrome:").Append(surface).Append(";--cm-chrome-fg:").Append(muted).Append(";--cm-chrome-title:").Append(title).Append(";--cm-tab:").Append(tab)
                .Append(";--cm-desk:").Append(light ? "#c9c9cf" : "#3a3a3f").Append(";--cm-close:").Append(light ? "#ff5f57" : "#ce534c")
                .Append(";--cm-minimise:").Append(light ? "#febc2e" : "#c59a2a").Append(";--cm-zoom:").Append(light ? "#28c840" : "#26a443").Append("}\n");
        }

        internal static void AppendSvg(StringBuilder svg, double width, double height, string? title, WindowChromeStyle style) {
            svg.Append("<rect width=\"").Append(ExportText.Num(width)).Append("\" height=\"").Append(ExportText.Num(height)).Append("\" rx=\"").Append(Radius(style))
                .Append("\" style=\"fill:var(--cm-chrome)\"/>\n");
            if (style == WindowChromeStyle.MacOS) {
                for (int i = 0; i < 3; i++) {
                    svg.Append("<circle cx=\"").Append(18 + i * 20).Append("\" cy=\"14\" r=\"6\" style=\"fill:var(--cm-")
                        .Append(i == 0 ? "close" : i == 1 ? "minimise" : "zoom").Append(")\"/>");
                }
            } else {
                if (style == WindowChromeStyle.Windows) {
                    svg.Append("<rect x=\"4\" y=\"4\" width=\"").Append(ExportText.Num(width - 144)).Append("\" height=\"28\" rx=\"6\" style=\"fill:var(--cm-tab)\"/>");
                    AppendPrompt(svg);
                }
                AppendRight(svg, width, style);
            }
            double available = Math.Max(0, width - Left(style) - Right(style));
            string visible = TextWrapper.Truncate(title ?? string.Empty, (int)Math.Floor(available / TitleAdvance), TerminalWidthProfile.Standard);
            if (visible.Length == 0) { return; }
            svg.Append("<svg class=\"cm-title-area\" x=\"").Append(Left(style)).Append("\" y=\"0\" width=\"").Append(ExportText.Num(available))
                .Append("\" height=\"").Append(BarHeight(style)).Append("\" overflow=\"hidden\"><text class=\"cm-title\" x=\"").Append(ExportText.Num(style == WindowChromeStyle.Windows ? 0 : available / 2))
                .Append("\" y=\"").Append(BarHeight(style) / 2).Append("\" textLength=\"").Append(DisplayWidth.Measure(visible, TerminalWidthProfile.Standard) * TitleAdvance)
                .Append("\" lengthAdjust=\"spacingAndGlyphs\" style=\"font-family:monospace;").Append(style == WindowChromeStyle.Windows ? "text-anchor:start;" : "")
                .Append(style == WindowChromeStyle.Linux ? "font-weight:bold;" : "").Append("\"><title>");
            ExportText.AppendEscaped(svg, title);
            svg.Append("</title>"); ExportText.AppendEscaped(svg, visible); svg.Append("</text></svg>\n");
        }

        private static void AppendPrompt(StringBuilder svg) => svg.Append("<path d=\"M13 11l5 5-5 5m8 0h6\" fill=\"none\" stroke=\"var(--cm-chrome-fg)\" stroke-width=\"1.3\"/>");

        private static void AppendRight(StringBuilder svg, double width, WindowChromeStyle style) {
            string x = ExportText.Num(width - (style == WindowChromeStyle.Windows ? 140 : 48));
            svg.Append("<g transform=\"translate(").Append(x).Append(" 0)\" fill=\"none\" stroke=\"var(--cm-chrome-fg)\" stroke-width=\"1.2\">");
            if (style == WindowChromeStyle.Windows) {
                svg.Append("<path d=\"M11 11v10m-5-5h10M44 18h10M116 11l10 10m0-10l-10 10\"/><rect x=\"80\" y=\"11\" width=\"10\" height=\"10\"/>");
            } else {
                svg.Append("<circle cx=\"26\" cy=\"18\" r=\"11\"/><path d=\"M22 14l8 8m0-8l-8 8\"/>");
            }
            svg.Append("</g>");
        }
    }
}
