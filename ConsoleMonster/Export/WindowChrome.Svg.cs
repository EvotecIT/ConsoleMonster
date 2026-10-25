using System;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Export {
    internal static partial class WindowChrome {
        /// <summary>Draws the page and window before body layers. Raster shadows are painted, never filtered.</summary>
        internal static void AppendSvg(StringBuilder svg, double width, double height, string? title, WindowChromeStyle style, bool raster = false, double titleAscent = 13) {
            double radius = Radius(style), bar = BarHeight(style);
            Rect(svg, 0, 0, width, height, 0, "var(--cm-desk)");
            if (raster) {
                Rect(svg, 12, 18, width - 24, height - 24, radius + 2, "#0000002e");
                Rect(svg, 10, 14, width - 20, height - 20, radius + 1, "#0000001f");
            } else {
                svg.Append("<defs><filter id=\"cm-shadow\" x=\"-15%\" y=\"-15%\" width=\"130%\" height=\"140%\"><feDropShadow dx=\"0\" dy=\"12\" stdDeviation=\"18\" flood-color=\"#000000\" flood-opacity=\"0.28\"/></filter></defs>");
                svg.Append("<g filter=\"url(#cm-shadow)\">");
                Rect(svg, 8, 8, width - 16, height - 16, radius, "var(--cm-bg)");
                svg.Append("</g>");
            }
            Rect(svg, 8, 8, width - 16, height - 16, radius, "var(--cm-bg)", "var(--cm-border)", 1.2);
            svg.Append("<defs><clipPath id=\"cm-inner-clip\">");
            Rect(svg, 9, 9, width - 18, height - 18, radius - 1, "#ffffff");
            svg.Append("</clipPath><clipPath id=\"cm-bar-clip\">");
            Rect(svg, 8, 8, width - 16, height - 16, radius, "#ffffff");
            svg.Append("</clipPath></defs><g clip-path=\"url(#cm-bar-clip)\">");
            Rect(svg, 8, 8, width - 16, bar, 0, "var(--cm-chrome)");
            svg.Append("</g>");
            svg.Append("<path d=\"M8 ").Append(ExportText.Num(bar + 8)).Append('H').Append(ExportText.Num(width - 8))
                .Append("\" fill=\"none\" stroke=\"var(--cm-border)\" stroke-width=\"1\"/>");
            if (style == WindowChromeStyle.MacOS) {
                for (int i = 0; i < 3; i++) {
                    svg.Append("<circle cx=\"").Append(26 + i * 20).Append("\" cy=\"22\" r=\"6\" style=\"fill:var(--cm-")
                        .Append(i == 0 ? "close" : i == 1 ? "minimise" : "zoom").Append(")\"/>");
                }
            } else if (style == WindowChromeStyle.Windows) {
                double tab = TabWidth(width);
                Rect(svg, 16, 12, tab, 28, 6, "var(--cm-tab)");
                svg.Append("<g transform=\"translate(28 17)\">");
                AppendPrompt(svg);
                svg.Append("</g>");
                Cross(svg, 16 + tab - 19, 26, 4, "var(--cm-chrome-fg)");
                AppendTabActions(svg, 16 + tab);
                AppendRight(svg, width, style);
            } else {
                AppendRight(svg, width, style);
            }
            double available = style == WindowChromeStyle.Windows ? Math.Max(8, TabWidth(width) - 72) : Math.Max(0, width - (style == WindowChromeStyle.MacOS ? 168 : 112));
            string visible = TextWrapper.Truncate(title ?? string.Empty, (int)Math.Floor(available / TitleAdvance), TerminalWidthProfile.Standard);
            if (visible.Length == 0) { return; }
            double left = style == WindowChromeStyle.Windows ? 56 : (width - available) / 2;
            svg.Append("<svg class=\"cm-title-area\" x=\"").Append(ExportText.Num(left)).Append("\" y=\"0\" width=\"").Append(ExportText.Num(available))
                .Append("\" height=\"").Append(bar + 8).Append("\" overflow=\"hidden\"><text class=\"cm-title\" x=\"").Append(ExportText.Num(style == WindowChromeStyle.Windows ? 0 : available / 2))
                .Append("\" y=\"").Append(ExportText.Num((style == WindowChromeStyle.Windows ? 18 : 8 + bar / 2 - 6.5) + titleAscent))
                .Append("\" dominant-baseline=\"alphabetic\" style=\"font-size:13px;font-weight:400;text-anchor:").Append(style == WindowChromeStyle.Windows ? "start" : "middle").Append("\"><title>");
            ExportText.AppendEscaped(svg, title);
            svg.Append("</title>"); ExportText.AppendEscaped(svg, visible); svg.Append("</text></svg>\n");
        }

        private static void Rect(StringBuilder svg, double x, double y, double width, double height, double radius, string fill, string? stroke = null, double strokeWidth = 0) {
            svg.Append("<rect x=\"").Append(ExportText.Num(x)).Append("\" y=\"").Append(ExportText.Num(y)).Append("\" width=\"").Append(ExportText.Num(Math.Max(0, width)))
                .Append("\" height=\"").Append(ExportText.Num(Math.Max(0, height))).Append("\" rx=\"").Append(ExportText.Num(radius)).Append("\" fill=\"").Append(fill).Append('"');
            if (stroke != null) { svg.Append(" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(ExportText.Num(strokeWidth)).Append('"'); }
            svg.Append("/>");
        }

        private static void AppendPrompt(StringBuilder svg) {
            Rect(svg, 0, 0, 18, 18, 3, "var(--cm-accent)");
            svg.Append("<path d=\"M4 5l4 4-4 4M9.5 13h5\" fill=\"none\" stroke=\"var(--cm-bg)\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        }

        private static void Cross(StringBuilder svg, double x, double y, double radius, string paint) =>
            svg.Append("<path d=\"M").Append(ExportText.Num(x - radius)).Append(' ').Append(ExportText.Num(y - radius)).Append('l').Append(ExportText.Num(radius * 2)).Append(' ').Append(ExportText.Num(radius * 2))
                .Append('m').Append(ExportText.Num(-radius * 2)).Append(" 0l").Append(ExportText.Num(radius * 2)).Append(' ').Append(ExportText.Num(-radius * 2))
                .Append("\" fill=\"none\" stroke=\"").Append(paint).Append("\" stroke-width=\"1.4\"/>");

        private static void AppendTabActions(StringBuilder svg, double right) {
            svg.Append("<g transform=\"translate(").Append(ExportText.Num(right)).Append(" 0)\" fill=\"none\" stroke=\"var(--cm-chrome-fg)\">")
                .Append("<path d=\"M19 26h12M25 20v12\" stroke-width=\"1.4\"/><path d=\"M52 23l4 4 4-4\" stroke-width=\"1.5\"/></g>");
        }

        private static void AppendRight(StringBuilder svg, double width, WindowChromeStyle style) {
            if (style == WindowChromeStyle.Windows) {
                svg.Append("<g transform=\"translate(").Append(ExportText.Num(width)).Append(" 0)\" fill=\"none\" stroke=\"var(--cm-fg)\" stroke-width=\"1.3\">")
                    .Append("<path d=\"M-106 26h12\"/><rect x=\"-66\" y=\"21\" width=\"12\" height=\"12\"/></g>");
                Cross(svg, width - 25, 26, 6, "var(--cm-fg)");
            } else {
                svg.Append("<circle cx=\"").Append(ExportText.Num(width - 30)).Append("\" cy=\"26\" r=\"11\" fill=\"none\" stroke=\"var(--cm-chrome-fg)\" stroke-width=\"1.2\"/>");
                Cross(svg, width - 30, 26, 4, "var(--cm-chrome-fg)");
            }
        }
    }
}
