using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace SiteFrames.Frames {
    /// <summary>The terminal a frame is shown in: its default foreground and background.</summary>
    internal sealed class Screen {
        private Screen(string name, string foreground, string background) {
            Name = name;
            Foreground = foreground;
            Background = background;
        }

        /// <summary>A dark terminal (the Windows Terminal "Campbell" defaults ConsoleMonster's exporter uses).</summary>
        public static Screen Dark { get; } = new Screen("dark", "#cccccc", "#0c0c0c");

        /// <summary>A light terminal.</summary>
        public static Screen Light { get; } = new Screen("light", "#24292f", "#ffffff");

        public string Name { get; }

        public string Foreground { get; }

        public string Background { get; }
    }

    /// <summary>
    /// Converts a rendered <see cref="CellBuffer"/> into HTML for a <c>pre.cm-screen</c>: one text line per row, styled runs as
    /// spans whose classes name their colours (so the stylesheet is shared by every frame on the site), and box drawing,
    /// block elements and braille painted on the cell grid by <see cref="GlyphPaint"/> so lines join whatever font the
    /// viewer has. The text stays selectable and copies as the terminal showed it.
    /// </summary>
    internal static class FrameHtml {
        /// <summary>Converts every row of a frame.</summary>
        /// <param name="frame">The cells.</param>
        /// <param name="depth">The colour depth the terminal had: colours are reduced exactly as ConsoleMonster's SGR encoder reduces them.</param>
        /// <param name="screen">The terminal defaults.</param>
        /// <param name="registry">Collects the classes used.</param>
        /// <returns>One HTML string per row.</returns>
        public static string[] Rows(CellBuffer frame, ColorDepth depth, Screen screen, FrameStyles registry) {
            string[] rows = new string[frame.Height];
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < frame.Height; y++) {
                sb.Clear();
                AppendRow(sb, frame, y, depth, screen, registry);
                rows[y] = sb.ToString();
            }
            return rows;
        }

        /// <summary>The rows of a frame joined into the content of a <c>pre</c>, with trailing empty rows removed.</summary>
        public static string Body(string[] rows, bool trimTrailing = true) {
            int count = rows.Length;
            if (trimTrailing) {
                while (count > 1 && rows[count - 1].Length == 0) {
                    count--;
                }
            }
            return string.Join("\n", rows, 0, count);
        }

        // One piece of a styled run: plain text, or an element (painted or boxed glyphs) with its class and width.
        private readonly struct Piece {
            public Piece(string text, string? cls = null, int width = 0) {
                Text = text;
                Class = cls;
                Width = width;
            }

            public string Text { get; }

            public string? Class { get; }

            public int Width { get; }
        }

        private static void AppendRow(StringBuilder sb, CellBuffer frame, int y, ColorDepth depth, Screen screen, FrameStyles registry) {
            int end = ContentEnd(frame, y, depth);
            int x = 0;
            List<Piece> pieces = new List<Piece>();
            while (x < end) {
                Cell first = frame[x, y];
                if (first.IsContinuation) {
                    x++;
                    continue;
                }
                Style style = Reduce(first.Style, depth);
                int runEnd = x + 1;
                while (runEnd < end && (frame[runEnd, y].IsContinuation || Reduce(frame[runEnd, y].Style, depth).Equals(style))) {
                    runEnd++;
                }
                pieces.Clear();
                Collect(pieces, frame, y, x, runEnd, registry);
                // Every run but the last is a box exactly as wide as its cells. Fonts round glyph advances, so text alone
                // would drift a fraction of a pixel per cell; sized boxes keep bars and borders aligned from row to row.
                bool last = runEnd >= end;
                string cells = last ? string.Empty : registry.CellsClass(runEnd - x);
                string classes = registry.ClassesFor(style, screen);
                string? link = style.Link != null && IsSafeLink(style.Link) ? style.Link : null;
                bool single = pieces.Count == 1 && pieces[0].Class != null;
                if (link != null) {
                    sb.Append("<a href=\"");
                    Escape(sb, link);
                    sb.Append("\" rel=\"nofollow noopener\"");
                    string all = Join(classes, cells);
                    if (all.Length > 0) {
                        sb.Append(" class=");
                        Attribute(sb, all);
                    }
                    sb.Append('>');
                    AppendPieces(sb, pieces, null, registry);
                    sb.Append("</a>");
                } else if (single) {
                    // A run that is one element carries the run's classes itself.
                    AppendPieces(sb, pieces, classes.Length > 0 ? classes : null, registry);
                } else if (classes.Length > 0 || cells.Length > 0) {
                    sb.Append("<span class=");
                    Attribute(sb, Join(classes, cells));
                    sb.Append('>');
                    AppendPieces(sb, pieces, null, registry);
                    sb.Append("</span>");
                } else {
                    AppendPieces(sb, pieces, null, registry);
                }
                x = runEnd;
            }
        }

        private static string Join(string a, string b) => a.Length == 0 ? b : b.Length == 0 ? a : a + " " + b;
        private static void Attribute(StringBuilder sb, string value) {
            if (value.IndexOf(' ') < 0) {
                sb.Append(value);
            } else {
                sb.Append('"').Append(value).Append('"');
            }
        }

        private static void AppendPieces(StringBuilder sb, List<Piece> pieces, string? extra, FrameStyles registry) {
            foreach (Piece piece in pieces) {
                if (piece.Class == null) {
                    Escape(sb, piece.Text);
                    continue;
                }
                string cls = piece.Width > 1 ? piece.Class + " " + registry.CellsClass(piece.Width) : piece.Class;
                sb.Append("<i class=");
                Attribute(sb, extra == null ? cls : cls + " " + extra);
                sb.Append('>');
                Escape(sb, piece.Text);
                sb.Append("</i>");
            }
        }

        private static void Collect(List<Piece> pieces, CellBuffer frame, int y, int from, int to, FrameStyles registry) {
            StringBuilder text = new StringBuilder();
            void Flush() {
                if (text.Length > 0) {
                    pieces.Add(new Piece(text.ToString()));
                    text.Clear();
                }
            }
            for (int i = from; i < to; i++) {
                Cell cell = frame[i, y];
                if (cell.IsContinuation) {
                    continue;
                }
                string glyph = string.IsNullOrEmpty(cell.Glyph) ? " " : cell.Glyph;
                char c = glyph.Length == 1 ? glyph[0] : '\0';
                if (glyph.Length == 1 && c >= 0x20 && c <= 0x7E) {
                    text.Append(c);
                    continue;
                }
                Flush();
                string? paint = glyph.Length == 1 ? registry.PaintFor(c) : null;
                if (paint != null) {
                    // A row of the same horizontally uniform glyph (a border, a bar) is painted as one element.
                    int same = i + 1;
                    if (GlyphPaint.Repeats(c)) {
                        while (same < to && frame[same, y].Width == 1 && frame[same, y].Glyph == glyph) {
                            same++;
                        }
                    }
                    pieces.Add(new Piece(new string(c, same - i), paint, same - i));
                    i = same - 1;
                } else if (c == '\u2800') {
                    // The empty braille pattern: nothing to paint, but keep the character for copying.
                    pieces.Add(new Piece(glyph, "n"));
                } else {
                    // A font may draw anything else at another width; box it to its cells so the grid never drifts.
                    pieces.Add(new Piece(glyph, cell.Width == 2 ? "w" : "n"));
                }
            }
            Flush();
        }
        /// <summary>The style a terminal of this depth receives (what ConsoleMonster's SGR encoder sends).</summary>
        public static Style Reduce(Style style, ColorDepth depth) {
            return depth == ColorDepth.None ? Style.Plain.WithLink(style.Link) : style.Downsample(depth);
        }

        /// <summary>The column after the last cell that shows anything.</summary>
        internal static int ContentEnd(CellBuffer frame, int y, ColorDepth depth) {
            for (int x = frame.Width - 1; x >= 0; x--) {
                Cell cell = frame[x, y];
                if (cell.IsContinuation || (cell.Glyph != " " && !string.IsNullOrEmpty(cell.Glyph))) {
                    return x + 1;
                }
                if (depth == ColorDepth.None) {
                    continue;
                }
                Style s = cell.Style;
                if (!s.BackgroundColor.IsDefault || (s.Decoration & (TextDecoration.Underline | TextDecoration.Strikethrough | TextDecoration.Reverse)) != 0) {
                    return x + 1;
                }
            }
            return 0;
        }

        internal static bool IsSafeLink(string link) {
            return link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || link.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }

        internal static void Escape(StringBuilder sb, string text) {
            foreach (char c in text) {
                switch (c) {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default:
                        if (c < 0x20 && c != '\t') {
                            sb.Append(' ');
                        } else {
                            sb.Append(c);
                        }
                        break;
                }
            }
        }

        internal static string Hex(byte r, byte g, byte b) {
            return r.ToString("x2", CultureInfo.InvariantCulture) + g.ToString("x2", CultureInfo.InvariantCulture) + b.ToString("x2", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The classes frames use, collected across the whole run so one generated stylesheet serves every frame. Colour
    /// classes are named after their value (<c>f60a5fa</c> foreground, <c>k1f2a3c</c> background), so a class name never
    /// depends on the order frames were rendered in.
    /// </summary>
    internal sealed class FrameStyles {
        private readonly SortedSet<string> _foregrounds = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _backgrounds = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<char> _painted = new SortedSet<char>();
        private readonly SortedSet<string> _glyphs = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<int> _cells = new SortedSet<int>();

        /// <summary>The class that makes a box exactly so many cells wide.</summary>
        public string CellsClass(int cells) {
            _cells.Add(cells);
            return "c" + cells.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Every glyph frames used (for font subsetting and coverage reports).</summary>
        public IEnumerable<string> Glyphs => _glyphs;

        public string ClassesFor(Style style, Screen screen) {
            string? fg = Css(style.Color);
            string? bg = Css(style.BackgroundColor);
            TextDecoration d = style.Decoration;
            bool reverse = (d & TextDecoration.Reverse) != 0;
            List<string> classes = new List<string>(4);
            if (reverse) {
                if (fg == null && bg == null) {
                    classes.Add("rv");
                } else {
                    string newFg = bg ?? screen.Background.Substring(1);
                    string newBg = fg ?? screen.Foreground.Substring(1);
                    fg = newFg;
                    bg = newBg;
                }
            }
            if ((d & TextDecoration.Dim) != 0 && !classes.Contains("rv")) {
                fg = Mix(bg ?? screen.Background.Substring(1), fg ?? screen.Foreground.Substring(1), 0.6);
            }
            if (fg != null && !classes.Contains("rv")) {
                _foregrounds.Add(fg);
                classes.Add("f" + fg);
            }
            if (bg != null && !classes.Contains("rv")) {
                _backgrounds.Add(bg);
                classes.Add("k" + bg);
            }
            if ((d & TextDecoration.Bold) != 0) {
                classes.Add("B");
            }
            if ((d & TextDecoration.Italic) != 0) {
                classes.Add("I");
            }
            if ((d & TextDecoration.Underline) != 0) {
                classes.Add((d & TextDecoration.Strikethrough) != 0 ? "US" : "U");
            } else if ((d & TextDecoration.Strikethrough) != 0) {
                classes.Add("S");
            }
            return string.Join(" ", classes);
        }

        public string? PaintFor(char c) {
            if (GlyphPaint.Has(c)) {
                _painted.Add(c);
                return "g" + ((int)c).ToString("x4", CultureInfo.InvariantCulture);
            }
            return null;
        }

        public void NoteGlyph(string glyph) => _glyphs.Add(glyph);

        private static string? Css(Color color) {
            if (color.IsDefault) {
                return null;
            }
            color.ToRgb(out byte r, out byte g, out byte b);
            return FrameHtml.Hex(r, g, b);
        }

        private static string Mix(string from, string to, double amount) {
            int fr = int.Parse(from.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int fg = int.Parse(from.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int fb = int.Parse(from.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int tr = int.Parse(to.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int tg = int.Parse(to.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int tb = int.Parse(to.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return FrameHtml.Hex(M(fr, tr, amount), M(fg, tg, amount), M(fb, tb, amount));
        }

        private static byte M(int a, int b, double t) => (byte)Math.Round(a + ((b - a) * t), MidpointRounding.AwayFromZero);

        /// <summary>The generated stylesheet: colour classes, decorations and the painted glyphs in use.</summary>
        public string Css() {
            StringBuilder css = new StringBuilder();
            css.Append("/* Generated by Website/tools/SiteFrames. Do not edit; rerun the tool. */\n");
            foreach (string fg in _foregrounds) {
                css.Append(".cm-screen .f").Append(fg).Append("{color:#").Append(fg).Append("}\n");
            }
            foreach (string bg in _backgrounds) {
                css.Append(".cm-screen .k").Append(bg).Append("{background-color:#").Append(bg).Append("}\n");
            }
            foreach (int n in _cells) {
                // A cell is 0.6em (the JetBrains Mono advance); em keeps widths exact where ch would be rounded per box.
                css.Append(".cm-screen .c").Append(n.ToString(CultureInfo.InvariantCulture)).Append("{width:").Append((n * 0.6).ToString("0.###", CultureInfo.InvariantCulture)).Append("em}\n");
            }
            foreach (char c in _painted) {
                css.Append(".cm-screen .g").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)).Append('{').Append(GlyphPaint.Rule(c)).Append("}\n");
            }
            return css.ToString();
        }
    }
}
