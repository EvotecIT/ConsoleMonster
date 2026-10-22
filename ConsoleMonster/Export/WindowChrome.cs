using System;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Text;

namespace ConsoleMonster.Export {
    /// <summary>Shared chrome bounds and native templates for static and recorded frame exports.</summary>
    internal static class WindowChrome {
        /// <summary>Three 12px dots at x=12,32,52, followed by 12px of clearance.</summary>
        internal const double MinimumWidth = 76;
        private const double RightInset = 12;
        // A fixed title advance makes standalone SVG fitting independent of installed font metrics.
        private const double TitleAdvance = 8;

        internal static string HtmlStart(string? title) => ConsoleExporter.ReadAsset("window-start.html")
            .TrimEnd('\r', '\n').Replace("__TITLE__", ExportText.Escape(title));

        internal static void AppendSvg(StringBuilder svg, double width, double height, string? title) {
            double available = Math.Max(0, width - MinimumWidth - RightInset);
            string visible = TextWrapper.Truncate(title ?? string.Empty, (int)Math.Floor(available / TitleAdvance), TerminalWidthProfile.Standard);
            string caption = string.Empty;
            if (visible.Length > 0) {
                // Substitute once so literal template-like text in a caller's title remains literal.
                caption = Regex.Replace(ConsoleExporter.ReadAsset("window-title.svg").TrimEnd('\r', '\n'), "__[A-Z]+__", token => token.Value switch {
                    "__WIDTH__" => ExportText.Num(available),
                    "__CENTRE__" => ExportText.Num(available / 2),
                    "__LENGTH__" => ExportText.Num(DisplayWidth.Measure(visible, TerminalWidthProfile.Standard) * TitleAdvance),
                    "__FULL__" => ExportText.Escape(title),
                    "__TEXT__" => ExportText.Escape(visible),
                    _ => token.Value
                });
            }
            svg.Append(ConsoleExporter.ReadAsset("window-chrome.svg")
                .Replace("__WIDTH__", ExportText.Num(width))
                .Replace("__HEIGHT__", ExportText.Num(height))
                .Replace("__TITLE__", caption));
        }
    }
}
