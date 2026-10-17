using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>One rendering of a frame with the palette that colours it (two layers when the theme follows the viewer).</summary>
    internal sealed class ExportLayer {
        public ExportLayer(CellBuffer frame, ExportPalette palette, string? scheme) {
            Frame = frame;
            Palette = palette;
            Scheme = scheme;
        }

        public CellBuffer Frame { get; }

        public ExportPalette Palette { get; }

        /// <summary>"dark" or "light" when the export carries both renderings; null for a single one.</summary>
        public string? Scheme { get; }
    }

    /// <summary>
    /// Writes frames as HTML: a <c>pre</c> per rendering, one text line per row, styled runs as inline blocks one line tall
    /// (so backgrounds meet without gaps), and glyphs a fallback font could draw at another width (wide, braille, symbols)
    /// in boxes exactly one or two cells wide, so the grid never drifts. Text stays selectable and copies as lines.
    /// </summary>
    internal static class HtmlFrameWriter {
        public static string Write(IReadOnlyList<ExportLayer> layers, ExportOptions options) {
            StringBuilder body = new StringBuilder();
            HtmlStyles styles = new HtmlStyles();
            int width = 0;
            foreach (ExportLayer layer in layers) {
                width = Math.Max(width, layer.Frame.Width);
            }
            if (options.WindowChrome) {
                body.Append("<div class=\"cm-window\"><div class=\"cm-titlebar\"><span class=\"cm-dots\"><i></i><i></i><i></i></span><span class=\"cm-title\">");
                ExportText.AppendEscaped(body, options.Title ?? string.Empty);
                body.Append("</span></div>");
            }
            foreach (ExportLayer layer in layers) {
                body.Append("<pre class=\"cm-screen");
                if (layer.Scheme != null) {
                    body.Append(" cm-").Append(layer.Scheme);
                }
                body.Append('"');
                if (!string.IsNullOrEmpty(options.Title)) {
                    body.Append(" aria-label=\"");
                    ExportText.AppendEscaped(body, options.Title);
                    body.Append('"');
                }
                body.Append('>');
                AppendRows(body, layer.Frame, layer.Palette, styles, (layer.Scheme ?? "s").Substring(0, 1));
                body.Append("</pre>");
            }
            if (options.WindowChrome) {
                body.Append("</div>");
            }

            StringBuilder css = new StringBuilder();
            AppendCss(css, layers, options, width);
            styles.AppendCss(css);

            StringBuilder html = new StringBuilder(body.Length + css.Length + 512);
            if (!options.Fragment) {
                html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
                html.Append("<meta name=\"color-scheme\" content=\"").Append(layers.Count > 1 || layers[0].Palette.UseVariables ? "dark light" : layers[0].Palette.Name).Append("\">\n");
                html.Append("<meta name=\"generator\" content=\"ConsoleMonster\">\n<title>");
                ExportText.AppendEscaped(html, string.IsNullOrEmpty(options.Title) ? "Console" : options.Title);
                html.Append("</title>\n");
            }
            html.Append("<style>\n").Append(css).Append("</style>\n");
            if (!options.Fragment) {
                html.Append("</head>\n<body>\n");
            }
            html.Append("<div class=\"cm-export\">").Append(body).Append("</div>\n");
            if (!options.Fragment) {
                html.Append("</body>\n</html>\n");
            }
            return html.ToString();
        }

        /// <summary>The CSS for the page, the window frame and the screen grid.</summary>
        internal static void AppendCss(StringBuilder css, IReadOnlyList<ExportLayer> layers, ExportOptions options, int columns) {
            string font = ExportText.IsSafeFontFamily(options.FontFamily) ? options.FontFamily : ExportOptions.DefaultFontFamily;
            double cell = options.CellHeight;
            bool adaptive = layers.Count > 1 || layers[0].Palette.UseVariables;
            ExportPalette first = layers[0].Palette.UseVariables ? ExportPalette.Dark : layers[0].Palette;
            string scope = options.Fragment ? ".cm-export" : ":root,.cm-export";
            css.Append(scope).Append("{--cm-fg:").Append(first.Foreground).Append(";--cm-bg:").Append(first.Background)
                .Append(";--cm-chrome:#2b2b2b;--cm-chrome-fg:#a8a8a8;--cm-desk:#3a3a3f;color-scheme:").Append(adaptive ? "dark light" : first.Name).Append("}\n.cm-export{display:inline-block}\n");
            if (!adaptive && first == ExportPalette.Light) {
                css.Append(scope).Append("{--cm-chrome:#e6e6e9;--cm-chrome-fg:#555;--cm-desk:#c9c9cf}\n");
            }
            if (adaptive) {
                css.Append("@media (prefers-color-scheme: light){").Append(scope).Append("{--cm-fg:").Append(ExportPalette.Light.Foreground).Append(";--cm-bg:").Append(ExportPalette.Light.Background)
                    .Append(";--cm-chrome:#e6e6e9;--cm-chrome-fg:#555;--cm-desk:#c9c9cf}}\n");
            }
            if (!options.Fragment) {
                // With a window frame the page is a neutral desk the window sits on; without one the page is the terminal.
                css.Append(options.WindowChrome ? "html,body{margin:0;background:var(--cm-desk)}\nbody{padding:20px}\n" : "html,body{margin:0;background:var(--cm-bg)}\nbody{padding:0}\n");
            }
            css.Append(".cm-export .cm-window{display:inline-block;border-radius:10px;overflow:hidden;box-shadow:0 10px 30px rgba(0,0,0,.35),0 0 0 1px rgba(127,127,127,.25);background:var(--cm-bg)}\n");
            css.Append(".cm-export .cm-titlebar{position:relative;height:30px;line-height:30px;background:var(--cm-chrome);color:var(--cm-chrome-fg);font:13px system-ui,-apple-system,'Segoe UI',sans-serif;text-align:center}\n");
            css.Append(".cm-export .cm-dots{position:absolute;left:12px;top:0}\n.cm-export .cm-dots i{display:inline-block;width:12px;height:12px;border-radius:50%;margin-right:8px;vertical-align:middle;background:#ff5f57}\n");
            css.Append(".cm-export .cm-dots i+i{background:#febc2e}\n.cm-export .cm-dots i+i+i{background:#28c840}\n");
            css.Append(".cm-export .cm-screen{display:block;box-sizing:content-box;margin:0;padding:").Append(options.Padding).Append("px;width:").Append(columns).Append("ch;")
                .Append("font-family:").Append(font).Append(";font-size:").Append(ExportText.Num(options.FontSize)).Append("px;line-height:").Append(ExportText.Num(cell)).Append("px;")
                .Append("color:var(--cm-fg);background:var(--cm-bg);white-space:pre;overflow:hidden;font-variant-ligatures:none;font-kerning:none;-webkit-text-size-adjust:none;text-size-adjust:none;tab-size:1}\n");
            css.Append(".cm-export .cm-screen span,.cm-export .cm-screen a{display:inline-block;vertical-align:top;height:").Append(ExportText.Num(cell)).Append("px}\n");
            css.Append(".cm-export .cm-screen a{color:inherit;text-decoration:none}\n.cm-export .cm-screen a:hover{text-decoration:underline}\n");
            css.Append(".cm-export .cm-screen .n{width:1ch;text-align:center;overflow:visible}\n.cm-export .cm-screen .w{width:2ch;text-align:center;overflow:visible}\n");
            if (layers.Count > 1) {
                css.Append(".cm-export .cm-light{display:none}\n@media (prefers-color-scheme: light){.cm-export .cm-dark{display:none}.cm-export .cm-light{display:block}}\n");
            }
        }

        /// <summary>Writes every row of a frame, separated by newlines (inside a <c>pre</c>).</summary>
        internal static void AppendRows(StringBuilder sb, CellBuffer frame, ExportPalette palette, HtmlStyles styles, string prefix) {
            for (int y = 0; y < frame.Height; y++) {
                if (y > 0) {
                    sb.Append('\n');
                }
                AppendRow(sb, frame, y, palette, styles, prefix);
            }
        }

        internal static void AppendRow(StringBuilder sb, CellBuffer frame, int y, ExportPalette palette, HtmlStyles styles, string prefix) {
            int end = ContentEnd(frame, y);
            int x = 0;
            while (x < end) {
                Cell first = frame[x, y];
                if (first.IsContinuation) {
                    x++;
                    continue;
                }
                Style style = first.Style;
                int runEnd = x;
                while (runEnd < end && (frame[runEnd, y].IsContinuation || frame[runEnd, y].Style.Equals(style))) {
                    runEnd++;
                }
                string? cls = styles.ClassFor(style, palette, prefix);
                string? link = style.Link != null && ExportText.IsSafeLink(style.Link) ? style.Link : null;
                if (link != null) {
                    sb.Append("<a href=\"");
                    ExportText.AppendEscaped(sb, link);
                    sb.Append('"');
                    if (cls != null) {
                        sb.Append(" class=\"").Append(cls).Append('"');
                    }
                    sb.Append('>');
                } else if (cls != null) {
                    sb.Append("<span class=\"").Append(cls).Append("\">");
                }
                for (int i = x; i < runEnd; i++) {
                    Cell cell = frame[i, y];
                    if (cell.IsContinuation) {
                        continue;
                    }
                    string? block = styles.BlockClass(cell.Glyph);
                    if (block != null) {
                        // Block elements are painted as cell backgrounds, so bars and charts have no seams between cells or
                        // rows whatever the font; the character stays (invisible) for copying.
                        sb.Append("<span class=\"b ").Append(block).Append("\">");
                        ExportText.AppendEscaped(sb, cell.Glyph);
                        sb.Append("</span>");
                    } else if (IsGridSafe(cell.Glyph)) {
                        ExportText.AppendEscaped(sb, cell.Glyph);
                    } else {
                        sb.Append(cell.Width == 2 ? "<span class=\"w\">" : "<span class=\"n\">");
                        ExportText.AppendEscaped(sb, cell.Glyph);
                        sb.Append("</span>");
                    }
                }
                if (link != null) {
                    sb.Append("</a>");
                } else if (cls != null) {
                    sb.Append("</span>");
                }
                x = runEnd;
            }
        }

        /// <summary>The column after the last cell that shows anything (text, a background, a line or a link).</summary>
        internal static int ContentEnd(CellBuffer frame, int y) {
            for (int x = frame.Width - 1; x >= 0; x--) {
                Cell cell = frame[x, y];
                if (cell.IsContinuation || cell.Glyph != " " || !cell.Style.Background.IsDefault || cell.Style.Link != null
                    || (cell.Style.Decoration & (TextDecoration.Underline | TextDecoration.Strikethrough | TextDecoration.Reverse)) != 0) {
                    return x + 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Glyphs every listed monospace font draws exactly one cell wide: printable ASCII, box drawing and block elements.
        /// Everything else is boxed to its cell width, because a fallback font may draw it wider or narrower.
        /// </summary>
        internal static bool IsGridSafe(string glyph) {
            if (glyph.Length != 1) {
                return false;
            }
            char c = glyph[0];
            return (c >= 0x20 && c <= 0x7E) || (c >= 0x2500 && c <= 0x259F);
        }
    }

    /// <summary>CSS classes for the distinct cell styles of a page (per rendering, so dark and light can differ).</summary>
    internal sealed class HtmlStyles {
        private readonly Dictionary<string, string> _classes = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<KeyValuePair<string, string>> _rules = new List<KeyValuePair<string, string>>();

        /// <summary>The class for a style, or null when it looks like the default text.</summary>
        public string? ClassFor(Style style, ExportPalette palette, string prefix) {
            palette.Resolve(style, out string fg, out string bg, out bool defaultBackground);
            TextDecoration d = style.Decoration;
            StringBuilder rule = new StringBuilder();
            if (fg != palette.Foreground) {
                rule.Append("color:").Append(fg).Append(';');
            } else if ((d & TextDecoration.Reverse) != 0) {
                rule.Append("color:").Append(fg).Append(';');
            }
            if (!defaultBackground || (d & TextDecoration.Reverse) != 0) {
                rule.Append("background:").Append(bg).Append(';');
            }
            if ((d & TextDecoration.Bold) != 0) {
                rule.Append("font-weight:bold;");
            }
            if ((d & TextDecoration.Italic) != 0) {
                rule.Append("font-style:italic;");
            }
            if ((d & TextDecoration.Dim) != 0 && fg.StartsWith("var(", StringComparison.Ordinal)) {
                rule.Append("opacity:.65;");
            }
            string lines = ((d & TextDecoration.Underline) != 0 ? "underline " : string.Empty) + ((d & TextDecoration.Strikethrough) != 0 ? "line-through" : string.Empty);
            if (lines.Length > 0) {
                rule.Append("text-decoration:").Append(lines.Trim()).Append(';');
            }
            if (rule.Length == 0) {
                return null;
            }
            string key = prefix + "|" + rule;
            if (!_classes.TryGetValue(key, out string? name)) {
                name = prefix + _classes.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _classes.Add(key, name);
                _rules.Add(new KeyValuePair<string, string>(name, rule.ToString()));
            }
            return name;
        }

        private readonly SortedDictionary<char, string> _blocks = new SortedDictionary<char, string>();

        /// <summary>
        /// The class that paints a block element (U+2580..U+259F) or a braille pattern (U+2801..U+28FF) as a background, or null
        /// for other glyphs. Braille drawn by fonts varies widely (and Consolas has none), so its dots are painted on the grid.
        /// </summary>
        public string? BlockClass(string glyph) {
            if (glyph.Length != 1 || !((glyph[0] >= '\u2580' && glyph[0] <= '\u259F') || (glyph[0] > '\u2800' && glyph[0] <= '\u28FF'))) {
                return null;
            }
            char c = glyph[0];
            if (!_blocks.TryGetValue(c, out string? name)) {
                name = "b" + ((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture);
                _blocks.Add(c, name);
            }
            return name;
        }

        public void AppendCss(StringBuilder css) {
            foreach (KeyValuePair<string, string> rule in _rules) {
                css.Append(".cm-export .").Append(rule.Key).Append('{').Append(rule.Value).Append("}\n");
            }
            if (_blocks.Count > 0) {
                css.Append(".cm-export .cm-screen .b{width:1ch;-webkit-text-fill-color:transparent;background-repeat:no-repeat}\n");
            }
            foreach (KeyValuePair<char, string> block in _blocks) {
                css.Append(".cm-export .").Append(block.Value).Append('{').Append(BlockRule(block.Key)).Append("}\n");
            }
        }

        private static string BlockRule(char c) {
            const string Solid = "linear-gradient(currentColor,currentColor)";
            if (c > '\u2800') {
                return BrailleRule(c - 0x2800);
            }
            switch (c) {
                case '\u2580': return "background:linear-gradient(currentColor 50%,transparent 0)";
                case '\u2588': return "background:currentColor";
                case '\u2590': return "background:linear-gradient(to left,currentColor 50%,transparent 0)";
                case '\u2591': return "background:currentColor;opacity:.25";
                case '\u2592': return "background:currentColor;opacity:.5";
                case '\u2593': return "background:currentColor;opacity:.75";
                case '\u2594': return "background:linear-gradient(currentColor 12.5%,transparent 0)";
                case '\u2595': return "background:linear-gradient(to left,currentColor 12.5%,transparent 0)";
            }
            if (c >= '\u2581' && c <= '\u2587') {
                return "background:linear-gradient(to top,currentColor " + Percent((c - '\u2580') / 8.0) + ",transparent 0)";
            }
            if (c >= '\u2589' && c <= '\u258F') {
                return "background:linear-gradient(to right,currentColor " + Percent(('\u2590' - c) / 8.0) + ",transparent 0)";
            }
            int[] quadrants = { 4, 8, 1, 1 | 4 | 8, 1 | 8, 1 | 2 | 4, 1 | 2 | 8, 2, 2 | 4, 2 | 4 | 8 };
            int mask = quadrants[c - '\u2596'];
            List<string> layers = new List<string>();
            if ((mask & 1) != 0) { layers.Add(Solid + " 0 0/50% 50%"); }
            if ((mask & 2) != 0) { layers.Add(Solid + " 100% 0/50% 50%"); }
            if ((mask & 4) != 0) { layers.Add(Solid + " 0 100%/50% 50%"); }
            if ((mask & 8) != 0) { layers.Add(Solid + " 100% 100%/50% 50%"); }
            return "background:" + string.Join(",", layers.ToArray()) + ";background-repeat:no-repeat";
        }

        // Dots 1-3 and 7 down the left column, 4-6 and 8 down the right, in a 2 by 4 grid (as the SVG writer draws them).
        private static string BrailleRule(int bits) {
            int[] column = { 0, 0, 0, 1, 1, 1, 0, 1 };
            int[] row = { 0, 1, 2, 0, 1, 2, 3, 3 };
            List<string> dots = new List<string>();
            for (int dot = 0; dot < 8; dot++) {
                if ((bits & (1 << dot)) == 0) {
                    continue;
                }
                string x = column[dot] == 0 ? "30%" : "70%";
                string y = Percent(((row[dot] * 2) + 1) / 8.0);
                dots.Add("radial-gradient(circle at " + x + " " + y + ",currentColor .09em,transparent .13em)");
            }
            return "background:" + string.Join(",", dots.ToArray());
        }

        private static string Percent(double fraction) => (fraction * 100).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
