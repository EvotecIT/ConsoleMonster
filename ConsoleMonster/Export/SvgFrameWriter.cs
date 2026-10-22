using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Writes frames as SVG: background runs as rectangles, box, block and braille characters as shapes, and text with an
    /// explicit centre for every glyph (each one is its own anchored chunk), so the grid holds in any font and text stays
    /// selectable. No external assets: the font is a family stack and everything else is inline.
    /// </summary>
    internal static class SvgFrameWriter {
        public const double TitleBarHeight = 30;

        public static string Write(IReadOnlyList<ExportLayer> layers, ExportOptions options) {
            SvgGeometry g = SvgGeometry.For(layers[0].Frame.Width, layers[0].Frame.Height, options);
            SvgStyles styles = new SvgStyles();
            StringBuilder content = new StringBuilder();
            foreach (ExportLayer layer in layers) {
                content.Append("<g class=\"cm-layer");
                if (layer.Scheme != null) {
                    content.Append(" cm-").Append(layer.Scheme);
                }
                content.Append("\" transform=\"translate(").Append(ExportText.Num(g.ScreenX)).Append(' ').Append(ExportText.Num(g.ScreenY)).Append(")\">");
                content.Append("<rect width=\"").Append(ExportText.Num(g.Width)).Append("\" height=\"").Append(ExportText.Num(g.GridHeight + (2 * options.Padding)))
                    .Append("\" x=\"").Append(-options.Padding).Append("\" y=\"").Append(-options.Padding).Append('"');
                ExportText.AppendPaint(content, "fill", layer.Palette.Background);
                content.Append("/>");
                AppendFrame(content, layer.Frame, layer.Palette, styles, (layer.Scheme ?? "s").Substring(0, 1), options);
                content.Append("</g>");
            }

            StringBuilder svg = new StringBuilder(content.Length + 2048);
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"").Append(ExportText.Num(g.Width)).Append("\" height=\"").Append(ExportText.Num(g.Height))
                .Append("\" viewBox=\"0 0 ").Append(ExportText.Num(g.Width)).Append(' ').Append(ExportText.Num(g.Height)).Append("\" role=\"img\"");
            if (!string.IsNullOrEmpty(options.Title)) {
                svg.Append(" aria-label=\"");
                ExportText.AppendEscaped(svg, options.Title);
                svg.Append('"');
            }
            svg.Append(">\n");
            if (!string.IsNullOrEmpty(options.Title)) {
                svg.Append("<title>");
                ExportText.AppendEscaped(svg, options.Title);
                svg.Append("</title>\n");
            }
            svg.Append("<style>\n");
            AppendCss(svg, layers, options);
            styles.AppendCss(svg);
            svg.Append("</style>\n");
            AppendChrome(svg, g, options);
            svg.Append(content);
            svg.Append("\n</svg>\n");
            return svg.ToString();
        }

        internal static void AppendCss(StringBuilder css, IReadOnlyList<ExportLayer> layers, ExportOptions options) {
            string font = ExportText.IsSafeFontFamily(options.FontFamily) ? options.FontFamily : ExportOptions.DefaultFontFamily;
            bool adaptive = layers.Count > 1 || layers[0].Palette.UseVariables;
            css.Append("svg{--cm-fg:").Append(ExportPalette.Dark.Foreground).Append(";--cm-bg:").Append(ExportPalette.Dark.Background).Append(";--cm-chrome:#1f1f1f;--cm-chrome-fg:#a0a0a0}\n");
            if (adaptive) {
                css.Append("@media (prefers-color-scheme: light){svg{--cm-fg:").Append(ExportPalette.Light.Foreground).Append(";--cm-bg:").Append(ExportPalette.Light.Background).Append(";--cm-chrome:#e8e8e8;--cm-chrome-fg:#555}}\n");
            } else if (layers[0].Palette == ExportPalette.Light) {
                css.Append("svg{--cm-chrome:#e8e8e8;--cm-chrome-fg:#555}\n");
            }
            css.Append(".cm-t{font-family:").Append(font).Append(";font-size:").Append(ExportText.Num(options.FontSize)).Append("px;text-anchor:middle;dominant-baseline:central;font-variant-ligatures:none}\n");
            css.Append(".cm-s{shape-rendering:crispEdges}\n.cm-title{font:13px system-ui,-apple-system,'Segoe UI',sans-serif;fill:var(--cm-chrome-fg);text-anchor:middle;dominant-baseline:central}\n");
            if (layers.Count > 1) {
                css.Append(".cm-light{display:none}\n@media (prefers-color-scheme: light){.cm-dark{display:none}.cm-light{display:inline}}\n");
            }
        }

        internal static void AppendChrome(StringBuilder svg, SvgGeometry g, ExportOptions options) {
            if (!options.WindowChrome) {
                return;
            }
            WindowChrome.AppendSvg(svg, g.Width, g.Height, options.Title);
        }

        /// <summary>Appends a frame's rows at the origin of the current group.</summary>
        internal static void AppendFrame(StringBuilder sb, CellBuffer frame, ExportPalette palette, SvgStyles styles, string prefix, ExportOptions options) {
            for (int y = 0; y < frame.Height; y++) {
                AppendRow(sb, frame, y, y * options.CellHeight, palette, styles, prefix, options);
            }
        }

        /// <summary>
        /// Appends one row at a vertical offset: background runs, then the shapes of box, block and braille characters (one
        /// path per colour), then text. Links wrap their text.
        /// </summary>
        internal static void AppendRow(StringBuilder sb, CellBuffer frame, int y, double top, ExportPalette palette, SvgStyles styles, string prefix, ExportOptions options) {
            double cw = options.CellWidth;
            double ch = options.CellHeight;
            int end = HtmlFrameWriter.ContentEnd(frame, y);
            SvgShapes backgrounds = new SvgShapes();
            SvgShapes shapes = new SvgShapes();
            StringBuilder texts = new StringBuilder();
            int x = 0;
            while (x < end) {
                Cell cell = frame[x, y];
                palette.Resolve(cell.Style, out string _, out string bg, out bool defaultBackground);
                if (defaultBackground && (cell.Style.Decoration & TextDecoration.Reverse) == 0) {
                    x++;
                    continue;
                }
                int run = x + 1;
                while (run < end) {
                    palette.Resolve(frame[run, y].Style, out string _, out string next, out bool nextDefault);
                    if (nextDefault && (frame[run, y].Style.Decoration & TextDecoration.Reverse) == 0 || next != bg) {
                        break;
                    }
                    run++;
                }
                backgrounds.Rect(x * cw, top, (run - x) * cw, ch, bg);
                x = run;
            }
            x = 0;
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
                palette.Resolve(style, out string fg, out string _, out bool _);
                shapes.Opacity = ExportPalette.ForegroundOpacity(style, fg);
                string? cls = styles.ClassFor(style, palette, prefix);
                string? link = style.Link != null && ExportText.IsSafeLink(style.Link) ? style.Link : null;
                StringBuilder chars = new StringBuilder();
                StringBuilder xs = new StringBuilder();
                StringBuilder separate = new StringBuilder();
                bool drawn = false;
                for (int i = x; i < runEnd; i++) {
                    Cell cell = frame[i, y];
                    if (cell.IsContinuation) {
                        continue;
                    }
                    double left = i * cw;
                    if (SvgGlyphs.Handles(cell.Glyph)) {
                        // Horizontal lines and full-width blocks repeated along a row are drawn as one shape.
                        int span = 1;
                        if (SvgGlyphs.Stretches(cell.Glyph)) {
                            while (i + span < runEnd && frame[i + span, y].Glyph == cell.Glyph && frame[i + span, y].Width == 1) {
                                span++;
                            }
                        }
                        SvgGlyphs.Append(shapes, cell.Glyph, left, top, cw * span, ch, fg);
                        drawn = true;
                        i += span - 1;
                        continue;
                    }
                    if (cell.Glyph == " ") {
                        continue;
                    }
                    drawn = true;
                    double centre = left + (cw * (cell.Width == 2 ? 1 : 0.5));
                    if (cell.Glyph.Length == 1) {
                        if (xs.Length > 0) {
                            xs.Append(' ');
                        }
                        xs.Append(ExportText.Num(centre));
                        ExportText.AppendEscaped(chars, cell.Glyph);
                    } else {
                        // A grapheme made of several code units gets its own element, so its position cannot shift the rest.
                        AppendText(separate, ExportText.Num(centre), ExportText.Escape(cell.Glyph), top + (ch / 2), cls, fg, palette);
                    }
                }
                if (drawn) {
                    double line = Math.Max(1, ch / 14);
                    if ((style.Decoration & TextDecoration.Underline) != 0) {
                        shapes.Rect(x * cw, top + ch - line - 1, (runEnd - x) * cw, line, fg);
                    }
                    if ((style.Decoration & TextDecoration.Strikethrough) != 0) {
                        shapes.Rect(x * cw, top + (ch / 2), (runEnd - x) * cw, line, fg);
                    }
                }
                StringBuilder run2 = new StringBuilder();
                run2.Append(separate);
                if (chars.Length > 0) {
                    AppendText(run2, xs.ToString(), chars.ToString(), top + (ch / 2), cls, fg, palette);
                }
                if (run2.Length > 0) {
                    if (link != null) {
                        texts.Append("<a href=\"");
                        ExportText.AppendEscaped(texts, link);
                        texts.Append("\" xlink:href=\"");
                        ExportText.AppendEscaped(texts, link);
                        texts.Append("\">").Append(run2).Append("</a>");
                    } else {
                        texts.Append(run2);
                    }
                }
                x = runEnd;
            }
            backgrounds.AppendTo(sb);
            shapes.AppendTo(sb);
            sb.Append(texts);
        }
        private static void AppendText(StringBuilder sb, string xs, string escaped, double y, string? cls, string fg, ExportPalette palette) {
            sb.Append("<text class=\"cm-t");
            if (cls != null) {
                sb.Append(' ').Append(cls);
            }
            sb.Append("\" x=\"").Append(xs).Append("\" y=\"").Append(ExportText.Num(y)).Append('"');
            if (cls == null) {
                ExportText.AppendPaint(sb, "fill", fg);
            }
            sb.Append('>').Append(escaped).Append("</text>");
        }
    }

    /// <summary>Sizes and offsets of an SVG export.</summary>
    internal sealed class SvgGeometry {
        public double GridWidth { get; private set; }

        public double GridHeight { get; private set; }

        public double ScreenX { get; private set; }

        public double ScreenY { get; private set; }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public static SvgGeometry For(int columns, int rows, ExportOptions options) {
            SvgGeometry g = new SvgGeometry {
                GridWidth = columns * options.CellWidth,
                GridHeight = rows * options.CellHeight
            };
            double chrome = options.WindowChrome ? SvgFrameWriter.TitleBarHeight : 0;
            g.ScreenX = options.Padding;
            g.ScreenY = options.Padding + chrome;
            g.Width = g.GridWidth + (2 * options.Padding);
            if (options.WindowChrome) {
                g.Width = Math.Max(g.Width, WindowChrome.MinimumWidth);
            }
            g.Height = g.GridHeight + (2 * options.Padding) + chrome;
            return g;
        }
    }

    /// <summary>CSS classes for text styles in an SVG export (fill, weight, slant).</summary>
    internal sealed class SvgStyles {
        private readonly Dictionary<string, string> _classes = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<KeyValuePair<string, string>> _rules = new List<KeyValuePair<string, string>>();

        public string? ClassFor(Style style, ExportPalette palette, string prefix) {
            palette.Resolve(style, out string fg, out string _, out bool _);
            TextDecoration d = style.Decoration;
            StringBuilder rule = new StringBuilder();
            rule.Append("fill:").Append(fg).Append(';');
            if ((d & TextDecoration.Bold) != 0) {
                rule.Append("font-weight:bold;");
            }
            if ((d & TextDecoration.Italic) != 0) {
                rule.Append("font-style:italic;");
            }
            if ((d & TextDecoration.Dim) != 0 && fg.StartsWith("var(", StringComparison.Ordinal)) {
                rule.Append("fill-opacity:.65;");
            }
            if (rule.Length == ("fill:" + fg + ";").Length) {
                return null;
            }
            string key = prefix + "|" + rule;
            if (!_classes.TryGetValue(key, out string? name)) {
                name = prefix + _classes.Count.ToString(CultureInfo.InvariantCulture);
                _classes.Add(key, name);
                _rules.Add(new KeyValuePair<string, string>(name, rule.ToString()));
            }
            return name;
        }

        public void AppendCss(StringBuilder css) {
            foreach (KeyValuePair<string, string> rule in _rules) {
                css.Append('.').Append(rule.Key).Append('{').Append(rule.Value).Append("}\n");
            }
        }
    }
}
