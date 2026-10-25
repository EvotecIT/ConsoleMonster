using System.Text;

namespace ConsoleMonster.Export {
    internal static partial class WindowChrome {
        internal static string HtmlStart(string? title, WindowChromeStyle style) =>
            "<svg class=\"cm-window-shadow\" xmlns=\"http://www.w3.org/2000/svg\" aria-hidden=\"true\"><defs><filter id=\"cm-html-shadow\" x=\"-15%\" y=\"-15%\" width=\"130%\" height=\"140%\"><feDropShadow dx=\"0\" dy=\"12\" stdDeviation=\"18\" flood-color=\"#000000\" flood-opacity=\"0.28\"/></filter></defs><rect width=\"100%\" height=\"100%\" rx=\"" + Radius(style) + "\" fill=\"var(--cm-bg)\" filter=\"url(#cm-html-shadow)\"/></svg>" +
            "<div class=\"cm-window cm-chrome-" + style.ToString().ToLowerInvariant() + "\">" + HtmlBar(title, style);

        internal static string HtmlBar(string? title, WindowChromeStyle style) {
            if (style == WindowChromeStyle.None) { return string.Empty; }
            StringBuilder html = new StringBuilder("<div class=\"cm-titlebar\">");
            string escaped = ExportText.Escape(title);
            if (style == WindowChromeStyle.MacOS) {
                html.Append("<span class=\"cm-dots\" aria-hidden=\"true\"><i></i><i></i><i></i></span>");
            } else if (style == WindowChromeStyle.Windows) {
                html.Append("<span class=\"cm-tab\"><svg class=\"cm-shell\" width=\"18\" height=\"18\" viewBox=\"0 0 18 18\" aria-hidden=\"true\">");
                AppendPrompt(html);
                html.Append("</svg><span class=\"cm-title\" title=\"").Append(escaped).Append("\">").Append(escaped).Append("</span>");
                html.Append("<svg class=\"cm-tab-close\" width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" aria-hidden=\"true\">");
                Cross(html, 8, 8, 4, "var(--cm-chrome-fg)");
                html.Append("</svg></span><svg class=\"cm-tab-actions\" width=\"68\" height=\"32\" viewBox=\"0 8 68 32\" aria-hidden=\"true\">");
                AppendTabActions(html, 0);
                html.Append("</svg>");
            }
            if (style != WindowChromeStyle.MacOS) {
                int width = style == WindowChromeStyle.Windows ? 140 : 48;
                html.Append("<svg class=\"cm-right\" width=\"").Append(width).Append("\" height=\"").Append(BarHeight(style)).Append("\" viewBox=\"0 8 ")
                    .Append(width).Append(' ').Append(BarHeight(style)).Append("\" aria-hidden=\"true\">");
                AppendRight(html, width + 8, style);
                html.Append("</svg>");
            }
            if (style != WindowChromeStyle.Windows) {
                html.Append("<span class=\"cm-title\" title=\"").Append(escaped).Append("\">").Append(escaped).Append("</span>");
            }
            return html.Append("</div>").ToString();
        }

        internal static void AppendHtmlCss(StringBuilder output, WindowChromeStyle style) {
            if (style == WindowChromeStyle.None) { return; }
            string window = ".cm-export .cm-window.cm-chrome-" + style.ToString().ToLowerInvariant();
            double radius = Radius(style), bar = BarHeight(style), reserve = style == WindowChromeStyle.MacOS ? 76 : 48;
            // The host owns the page inset; the window clips every screen layer at all four corners.
            output.Append(".cm-export:has(>.cm-window){padding:8px;background:var(--cm-desk);position:relative;isolation:isolate}\n");
            output.Append(".cm-export>.cm-window-shadow{position:absolute;left:8px;top:8px;width:calc(100% - 16px);height:calc(100% - 16px);overflow:visible;z-index:-1;pointer-events:none}\n.cm-export:has(>.cm-window-shadow)>.cm-window{filter:none}\n");
            output.Append(window).Append("{display:inline-block;position:relative;min-width:").Append(MinimumWidth(style)).Append("px;border-radius:").Append(radius)
                .Append("px;overflow:hidden;outline:1.2px solid var(--cm-border);outline-offset:-.6px;background:var(--cm-bg);filter:drop-shadow(0px 12px 18px rgba(0,0,0,.28))}\n");
            output.Append(window).Append(" .cm-screen{clip-path:inset(1px round 0 0 ").Append(radius - 1).Append("px ").Append(radius - 1).Append("px)}\n");
            output.Append(window).Append(" .cm-titlebar{position:relative;container-type:inline-size;min-width:").Append(MinimumWidth(style)).Append("px;height:").Append(bar)
                .Append("px;background:var(--cm-chrome);color:var(--cm-chrome-fg);box-shadow:inset 0 -1px var(--cm-border)}\n");
            output.Append(window).Append(" .cm-title{position:absolute;left:").Append(reserve).Append("px;right:").Append(reserve)
                .Append("px;top:calc(50% - 6.5px);display:block;min-width:0;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;color:var(--cm-chrome-title);text-align:center;font-family:var(--cm-font);font-size:13px;font-weight:400;line-height:13px}\n");
            output.Append("@container (width < ").Append(reserve * 2 + 8).Append("px){").Append(window).Append(" .cm-title{visibility:hidden}}\n");
            output.Append(window).Append(" .cm-right{position:absolute;top:0;right:0}\n");
            if (style == WindowChromeStyle.MacOS) {
                output.Append(window).Append(" .cm-dots{position:absolute;left:12px;top:8px;display:flex;gap:8px;height:12px}\n")
                    .Append(window).Append(" .cm-dots i{display:block;flex:none;width:12px;height:12px;border-radius:50%;background:var(--cm-close)}\n")
                    .Append(window).Append(" .cm-dots i+i{background:var(--cm-minimise)}\n")
                    .Append(window).Append(" .cm-dots i+i+i{background:var(--cm-zoom)}\n");
            } else if (style == WindowChromeStyle.Windows) {
                output.Append(window).Append(" .cm-tab{position:absolute;left:8px;top:4px;width:clamp(96px,calc(100cqw - 214px),300px);height:28px;border-radius:6px;background:var(--cm-tab)}\n")
                    .Append(window).Append(" .cm-tab .cm-title{left:40px;right:32px;top:6px;text-align:left;visibility:visible}\n")
                    .Append(window).Append(" .cm-shell{position:absolute;left:12px;top:5px}\n")
                    .Append(window).Append(" .cm-tab-close{position:absolute;right:11px;top:6px}\n")
                    .Append(window).Append(" .cm-tab-actions{position:absolute;left:calc(8px + clamp(96px,calc(100cqw - 214px),300px));top:0}\n");
            }
        }
    }
}
