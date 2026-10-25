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

        /// <summary>A blank terminal row supplies the outer background only when every cell has an explicit background.</summary>
        public string ScreenBackground {
            get {
                if (Frame.Width == 0 || Frame.Height == 0) { return Palette.Background; }
                Color background = Frame[0, Frame.Height - 1].Style.BackgroundColor;
                if (background.IsDefault) { return Palette.Background; }
                for (int x = 0; x < Frame.Width; x++) {
                    Cell cell = Frame[x, Frame.Height - 1];
                    if ((!string.IsNullOrEmpty(cell.Glyph) && cell.Glyph != " ") || cell.IsContinuation
                        || cell.Style.Decoration != TextDecoration.None || cell.Style.BackgroundColor != background) {
                        return Palette.Background;
                    }
                }
                // A footer cannot redefine terminal-default backgrounds elsewhere in the frame.
                for (int y = 0; y < Frame.Height; y++) {
                    for (int x = 0; x < Frame.Width; x++) {
                        if (Frame[x, y].Style.BackgroundColor.IsDefault) { return Palette.Background; }
                    }
                }
                return ExportPalette.ToCss(background)!;
            }
        }

        /// <summary>"dark" or "light" when the export carries both renderings; null for a single one.</summary>
        public string? Scheme { get; }
    }

    /// <summary>
    /// Writes frames as HTML: a <c>pre</c> per rendering, one text line per row, styled runs as inline blocks one line tall
    /// (so backgrounds meet without gaps), and glyphs a fallback font could draw at another width (wide, braille, symbols)
    /// in boxes exactly one or two cells wide, so the grid never drifts. Blocks share SVG's geometry in one screen-wide
    /// coordinate space, so adjacent rows snap to the same device pixels. Text stays selectable and copies as lines.
    /// </summary>
    internal static class HtmlFrameWriter {
        private static readonly string ScreenStart = ConsoleExporter.ReadAsset("screen-start.html").TrimEnd('\r', '\n');

        public static string Write(IReadOnlyList<ExportLayer> layers, ExportOptions options) {
            StringBuilder body = new StringBuilder();
            HtmlStyles styles = new HtmlStyles();
            int width = 0;
            foreach (ExportLayer layer in layers) {
                width = Math.Max(width, layer.Frame.Width);
            }
            if ((options.EffectiveChrome != WindowChromeStyle.None)) {
                body.Append(WindowChrome.HtmlStart(options.Title, options.EffectiveChrome));
            }
            foreach (ExportLayer layer in layers) {
                body.Append("<pre class=\"cm-screen ").Append(styles.ScreenHeightClass(layer.Frame.Height, options.CellHeight));
                if (layer.Scheme != null) {
                    body.Append(" cm-").Append(layer.Scheme);
                }
                body.Append('"');
                if (!string.IsNullOrEmpty(options.Title)) {
                    body.Append(" aria-label=\"");
                    ExportText.AppendEscaped(body, options.Title);
                    body.Append('"');
                }
                // A non-text token preserves an initial LF that HTML's pre parser would otherwise discard.
                body.Append('>').Append(ScreenStart);
                AppendRows(body, layer.Frame, layer.Palette, styles, (layer.Scheme ?? "s").Substring(0, 1));
                HtmlBlockPaint.AppendFrame(body, layer.Frame, layer.Palette);
                body.Append("</pre>");
            }
            if ((options.EffectiveChrome != WindowChromeStyle.None)) {
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
            WindowChrome.AppendTokens(css, scope, options, !adaptive && first == ExportPalette.Light);
            if (adaptive) {
                css.Append("@media (prefers-color-scheme: light){").Append(scope).Append("{--cm-fg:").Append(ExportPalette.Light.Foreground).Append(";--cm-bg:").Append(ExportPalette.Light.Background).Append("}\n");
                WindowChrome.AppendTokens(css, scope, options, true);
                css.Append("}\n");
            }
            if (!options.Fragment) {
                css.Append(options.EffectiveChrome != WindowChromeStyle.None ? "html,body{margin:0;background:var(--cm-desk)}\nbody{padding:20px}\n" : "html,body{margin:0;background:var(--cm-bg)}\nbody{padding:0}\n");
            }
            WindowChrome.AppendHtmlCss(css, options.EffectiveChrome);
            css.Append(".cm-export .cm-screen{display:block;box-sizing:content-box;margin:0;padding:").Append(options.Padding).Append("px;width:").Append(columns).Append("ch;")
                .Append("font-family:").Append(font).Append(";font-size:").Append(ExportText.Num(options.FontSize)).Append("px;line-height:").Append(ExportText.Num(cell)).Append("px;")
                .Append("color:var(--cm-fg);background:var(--cm-bg);white-space:pre;overflow:hidden;font-variant-ligatures:none;font-kerning:none;-webkit-text-size-adjust:none;text-size-adjust:none;tab-size:1}\n");
            css.Append(".cm-export .cm-screen span,.cm-export .cm-screen a{display:inline-block;vertical-align:top;height:").Append(ExportText.Num(cell)).Append("px}\n");
            css.Append(".cm-export .cm-screen a{color:inherit;text-decoration:none}\n.cm-export .cm-screen a:hover{text-decoration:underline}\n");
            css.Append(".cm-export .cm-screen .n{width:1ch;text-align:center;overflow:visible}\n.cm-export .cm-screen .w{width:2ch;text-align:center;overflow:visible}\n");
            css.Append(ConsoleExporter.ReadAsset("block.css")
                .Replace("__PADDING__", options.Padding.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("__INSET__", (options.Padding * 2).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            foreach (ExportLayer layer in layers) {
                if (layer.ScreenBackground != layer.Palette.Background) {
                    css.Append(".cm-export .cm-screen").Append(layer.Scheme == null ? string.Empty : ".cm-" + layer.Scheme)
                        .Append("{background:").Append(layer.ScreenBackground).Append("}\n");
                }
            }
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
                AppendRow(sb, frame, y, palette, styles, prefix, false);
            }
        }

        internal static void AppendRow(StringBuilder sb, CellBuffer frame, int y, ExportPalette palette, HtmlStyles styles, string prefix, bool paintBlocks = true) {
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
                        int count = 1;
                        if (SvgGlyphs.Stretches(cell.Glyph)) {
                            while (i + count < runEnd && frame[i + count, y].Glyph == cell.Glyph && frame[i + count, y].Width == 1) {
                                count++;
                            }
                        }
                        // Reuse SVG's grid geometry and device-pixel edge snapping: separate CSS background boxes can
                        // leave hairlines at fractional browser scales, even when their layout edges meet exactly.
                        sb.Append("<span class=\"b ").Append(block);
                        if (count > 1) {
                            sb.Append(' ').Append(styles.BlockWidthClass(count));
                        }
                        sb.Append("\">");
                        for (int j = 0; j < count; j++) {
                            ExportText.AppendEscaped(sb, cell.Glyph);
                        }
                        if (paintBlocks && cell.Glyph[0] < '\u2800') {
                            styles.AppendBlockPaint(sb, cell.Glyph, count);
                        }
                        sb.Append("</span>");
                        i += count - 1;
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
                if (cell.IsContinuation || cell.Glyph != " " || !cell.Style.BackgroundColor.IsDefault || cell.Style.Link != null
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
        private static readonly string BlockTemplate = ConsoleExporter.ReadAsset("block.svg").TrimEnd('\r', '\n');
        private static readonly string ScreenHeightTemplate = ConsoleExporter.ReadAsset("screen-height.css");
        private readonly Dictionary<string, string> _classes = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<KeyValuePair<string, string>> _rules = new List<KeyValuePair<string, string>>();
        private readonly SortedDictionary<int, double> _screenHeights = new SortedDictionary<int, double>();

        /// <summary>Reserves the full frame grid even when trailing newlines have no natural line box.</summary>
        public string ScreenHeightClass(int rows, double cellHeight) {
            _screenHeights[rows] = rows * cellHeight;
            return "cm-h" + rows.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

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
        private readonly SortedDictionary<int, string> _blockWidths = new SortedDictionary<int, string>();

        /// <summary>A grid width for a continuous block run; paint and copied text cover the same number of cells.</summary>
        public string BlockWidthClass(int cells) {
            if (!_blockWidths.TryGetValue(cells, out string? name)) {
                name = "bc" + cells.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _blockWidths.Add(cells, name);
            }
            return name;
        }

        /// <summary>Paints a block run with the same crisp geometry as SVG exports, keeping its text selectable.</summary>
        public void AppendBlockPaint(StringBuilder html, string glyph, int cells) {
            SvgShapes shapes = new SvgShapes();
            SvgGlyphs.Append(shapes, glyph, 0, 0, cells * 8, 16, "currentColor");
            StringBuilder paths = new StringBuilder();
            shapes.AppendTo(paths);
            html.Append(BlockTemplate
                .Replace("__WIDTH__", (cells * 8).ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("__SHAPES__", paths.ToString()));
        }

        /// <summary>
        /// The class for a painted block element (U+2580..U+259F) or braille pattern (U+2801..U+28FF), or null for other
        /// glyphs. Block text is invisible under the vector paint; braille dots use the existing grid backgrounds.
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
            foreach (KeyValuePair<int, double> height in _screenHeights) {
                css.Append(ScreenHeightTemplate.Replace("__CLASS__", "cm-h" + height.Key.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Replace("__HEIGHT__", ExportText.Num(height.Value)));
            }
            foreach (KeyValuePair<string, string> rule in _rules) {
                css.Append(".cm-export .").Append(rule.Key).Append('{').Append(rule.Value).Append("}\n");
            }
            if (_blocks.Count > 0) {
                css.Append(".cm-export .cm-screen .b{width:1ch;-webkit-text-fill-color:transparent;background-repeat:no-repeat}\n");
            }
            foreach (KeyValuePair<char, string> block in _blocks) {
                if (block.Key >= '\u2800') {
                    css.Append(".cm-export .").Append(block.Value).Append('{').Append(BrailleRule(block.Key - 0x2800)).Append("}\n");
                }
            }
            if (_blockWidths.Count > 0) {
                string template = ConsoleExporter.ReadAsset("block-run.css");
                foreach (KeyValuePair<int, string> width in _blockWidths) {
                    css.Append(template.Replace("__CLASS__", width.Value).Replace("__CELLS__", width.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
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
