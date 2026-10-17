using System;
using System.Text;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Draws box drawing, block element and braille characters as shapes on the cell grid, so SVG exports have no gaps
    /// between border lines, no seams in bars and correctly aligned braille charts whatever font the viewer has.
    /// Lines are drawn as rectangles from the cell centre to its edges (light, heavy or double), so neighbouring cells meet
    /// exactly; rounded corners are arcs; shades are blocks with partial opacity.
    /// </summary>
    internal static class SvgGlyphs {
        // Arms of U+2500..U+257F as four digits: up, right, down, left (0 none, 1 light, 2 heavy, 3 double). '-' marks a
        // character drawn separately (dashed lines, rounded corners, diagonals).
        private static readonly string[] Arms = BuildArms();

        private static string[] BuildArms() {
            string table =
                "0101 0202 1010 2020 - - - - - - - - 0110 0210 0120 0220 " +
                "0011 0012 0021 0022 1100 1200 2100 2200 1001 1002 2001 2002 1110 1210 2110 1120 " +
                "2120 2210 1220 2220 1011 1012 2011 1021 2021 2012 1022 2022 0111 0112 0211 0212 " +
                "0121 0122 0221 0222 1101 1102 1201 1202 2101 2102 2201 2202 1111 1112 1211 1212 " +
                "2111 1121 2121 2112 2211 1122 1221 2212 1222 2122 2221 2222 - - - - " +
                "0303 3030 0310 0130 0330 0013 0031 0033 1300 3100 3300 1003 3001 3003 1310 3130 " +
                "3330 1013 3031 3033 0313 0131 0333 1303 3101 3303 1313 3131 3333 - - - " +
                "- - - - 0001 1000 0100 0010 0002 2000 0200 0020 0201 1020 0102 2010";
            string[] parts = table.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 128) {
                throw new InvalidOperationException("Box drawing table must have 128 entries.");
            }
            return parts;
        }

        /// <summary>Whether a glyph is drawn as shapes.</summary>
        public static bool Handles(string glyph) {
            if (glyph.Length != 1) {
                return false;
            }
            char c = glyph[0];
            return (c >= 0x2500 && c <= 0x259F) || (c >= 0x2800 && c <= 0x28FF);
        }

        /// <summary>
        /// Whether drawing the glyph across several cells at once looks the same as drawing it in each cell: horizontal
        /// lines (light, heavy, double) and blocks that fill the cell's width.
        /// </summary>
        public static bool Stretches(string glyph) {
            if (glyph.Length != 1) {
                return false;
            }
            char c = glyph[0];
            return c == '\u2500' || c == '\u2501' || c == '\u2550' || (c >= '\u2580' && c <= '\u2588') || (c >= '\u2591' && c <= '\u2594');
        }

        /// <summary>Appends the shapes for a glyph in the cell at (x, y) of size w by h. Returns false when not handled.</summary>
        public static bool Append(StringBuilder sb, string glyph, double x, double y, double w, double h, string fill) {
            if (!Handles(glyph)) {
                return false;
            }
            char c = glyph[0];
            if (c >= 0x2800) {
                Braille(sb, c - 0x2800, x, y, w, h, fill);
                return true;
            }
            if (c >= 0x2580) {
                return Block(sb, c, x, y, w, h, fill);
            }
            return Box(sb, c, x, y, w, h, fill);
        }

        private static bool Box(StringBuilder sb, char c, double x, double y, double w, double h, string fill) {
            double light = Math.Max(1, Math.Round(h / 14.0, 2));
            double heavy = light * 2;
            double cx = x + (w / 2);
            double cy = y + (h / 2);
            switch (c) {
                case '┄': case '┈': case '╌': Dashes(sb, c == '┄' ? 3 : c == '┈' ? 4 : 2, true, x, y, w, h, light, fill); return true;
                case '┅': case '┉': case '╍': Dashes(sb, c == '┅' ? 3 : c == '┉' ? 4 : 2, true, x, y, w, h, heavy, fill); return true;
                case '┆': case '┊': case '╎': Dashes(sb, c == '┆' ? 3 : c == '┊' ? 4 : 2, false, x, y, w, h, light, fill); return true;
                case '┇': case '┋': case '╏': Dashes(sb, c == '┇' ? 3 : c == '┋' ? 4 : 2, false, x, y, w, h, heavy, fill); return true;
                case '╭': Arc(sb, cx, cy, x + w, cy, cx, y + h, w, h, light, fill, true, true); return true;
                case '╮': Arc(sb, cx, cy, x, cy, cx, y + h, w, h, light, fill, false, true); return true;
                case '╯': Arc(sb, cx, cy, x, cy, cx, y, w, h, light, fill, false, false); return true;
                case '╰': Arc(sb, cx, cy, x + w, cy, cx, y, w, h, light, fill, true, false); return true;
                case '╱': Line(sb, x + w, y, x, y + h, light, fill); return true;
                case '╲': Line(sb, x, y, x + w, y + h, light, fill); return true;
                case '╳': Line(sb, x + w, y, x, y + h, light, fill); Line(sb, x, y, x + w, y + h, light, fill); return true;
            }
            string arms = Arms[c - 0x2500];
            if (arms == "-") {
                return false;
            }
            int up = arms[0] - '0';
            int right = arms[1] - '0';
            int down = arms[2] - '0';
            int left = arms[3] - '0';
            double gap = light * 1.5;
            // Single and heavy arms run from the centre (plus half their thickness, so corners close) to the edge.
            if (left == 1 || left == 2) { double t = left == 2 ? heavy : light; Rect(sb, x, cy - (t / 2), cx + (Thick(up, down, light, heavy) / 2) - x, t, fill); }
            if (right == 1 || right == 2) { double t = right == 2 ? heavy : light; double from = cx - (Thick(up, down, light, heavy) / 2); Rect(sb, from, cy - (t / 2), x + w - from, t, fill); }
            if (up == 1 || up == 2) { double t = up == 2 ? heavy : light; Rect(sb, cx - (t / 2), y, t, cy + (Thick(left, right, light, heavy) / 2) - y, fill); }
            if (down == 1 || down == 2) { double t = down == 2 ? heavy : light; double from = cy - (Thick(left, right, light, heavy) / 2); Rect(sb, cx - (t / 2), from, t, y + h - from, fill); }
            // Double arms are two lines; where they meet another arm the inner line stops short, so joins read as double.
            if (right == 3) {
                double top = up != 0 ? cx + gap : cx - gap;
                double bottom = down != 0 ? cx + gap : cx - gap;
                Rect(sb, top, cy - gap - (light / 2), x + w - top, light, fill);
                Rect(sb, bottom, cy + gap - (light / 2), x + w - bottom, light, fill);
            }
            if (left == 3) {
                double top = up != 0 ? cx - gap : cx + gap;
                double bottom = down != 0 ? cx - gap : cx + gap;
                Rect(sb, x, cy - gap - (light / 2), top - x + (light / 2), light, fill);
                Rect(sb, x, cy + gap - (light / 2), bottom - x + (light / 2), light, fill);
            }
            if (up == 3) {
                double l = left != 0 ? cy - gap : cy + gap;
                double r = right != 0 ? cy - gap : cy + gap;
                Rect(sb, cx - gap - (light / 2), y, light, l - y + (light / 2), fill);
                Rect(sb, cx + gap - (light / 2), y, light, r - y + (light / 2), fill);
            }
            if (down == 3) {
                double l = left != 0 ? cy + gap : cy - gap;
                double r = right != 0 ? cy + gap : cy - gap;
                Rect(sb, cx - gap - (light / 2), l - (light / 2), light, y + h - l + (light / 2), fill);
                Rect(sb, cx + gap - (light / 2), r - (light / 2), light, y + h - r + (light / 2), fill);
            }
            return true;
        }

        // The thickness of the crossing arms, so a perpendicular arm reaches across them.
        private static double Thick(int a, int b, double light, double heavy) {
            int weight = Math.Max(a == 3 ? 1 : a, b == 3 ? 1 : b);
            return weight == 2 ? heavy : weight == 1 ? light : 0;
        }

        private static void Dashes(StringBuilder sb, int count, bool horizontal, double x, double y, double w, double h, double t, string fill) {
            double length = horizontal ? w : h;
            double step = length / count;
            double dash = step * 0.6;
            for (int i = 0; i < count; i++) {
                double start = (i * step) + ((step - dash) / 2);
                if (horizontal) {
                    Rect(sb, x + start, y + (h / 2) - (t / 2), dash, t, fill);
                } else {
                    Rect(sb, x + (w / 2) - (t / 2), y + start, t, dash, fill);
                }
            }
        }

        // A rounded corner: from the horizontal edge point to the centre, a quarter circle, then to the vertical edge point.
        private static void Arc(StringBuilder sb, double cx, double cy, double hx, double hy, double vx, double vy, double w, double h, double t, string fill, bool toRight, bool toBottom) {
            double r = Math.Min(w, h) / 2;
            double ax = toRight ? cx + r : cx - r;
            double ay = toBottom ? cy + r : cy - r;
            int sweep = (toRight == toBottom) ? 0 : 1;
            sb.Append("<path d=\"M").Append(ExportText.Num(hx)).Append(' ').Append(ExportText.Num(hy))
                .Append("H").Append(ExportText.Num(ax))
                .Append("A").Append(ExportText.Num(r)).Append(' ').Append(ExportText.Num(r)).Append(" 0 0 ").Append(sweep).Append(' ').Append(ExportText.Num(cx)).Append(' ').Append(ExportText.Num(ay))
                .Append("V").Append(ExportText.Num(vy))
                .Append("\" fill=\"none\" stroke-width=\"").Append(ExportText.Num(t)).Append('"');
            ExportText.AppendPaint(sb, "stroke", fill);
            sb.Append("/>");
        }

        private static void Line(StringBuilder sb, double x1, double y1, double x2, double y2, double t, string fill) {
            sb.Append("<line x1=\"").Append(ExportText.Num(x1)).Append("\" y1=\"").Append(ExportText.Num(y1)).Append("\" x2=\"").Append(ExportText.Num(x2)).Append("\" y2=\"").Append(ExportText.Num(y2))
                .Append("\" stroke-width=\"").Append(ExportText.Num(t)).Append('"');
            ExportText.AppendPaint(sb, "stroke", fill);
            sb.Append("/>");
        }

        private static bool Block(StringBuilder sb, char c, double x, double y, double w, double h, string fill) {
            switch (c) {
                case '▀': Rect(sb, x, y, w, h / 2, fill); return true;
                case '█': Rect(sb, x, y, w, h, fill); return true;
                case '▐': Rect(sb, x + (w / 2), y, w / 2, h, fill); return true;
                case '░': Rect(sb, x, y, w, h, fill, 0.25); return true;
                case '▒': Rect(sb, x, y, w, h, fill, 0.5); return true;
                case '▓': Rect(sb, x, y, w, h, fill, 0.75); return true;
                case '▔': Rect(sb, x, y, w, h / 8, fill); return true;
                case '▕': Rect(sb, x + (w * 7 / 8), y, w / 8, h, fill); return true;
            }
            if (c >= '▁' && c <= '▇') {
                double part = h * (c - '▀') / 8.0;
                Rect(sb, x, y + h - part, w, part, fill);
                return true;
            }
            if (c >= '▉' && c <= '▏') {
                double part = w * ('▐' - c) / 8.0;
                Rect(sb, x, y, part, h, fill);
                return true;
            }
            // Quadrants U+2596..U+259F: bits upper-left 1, upper-right 2, lower-left 4, lower-right 8.
            int[] quadrants = { 4, 8, 1, 1 | 4 | 8, 1 | 8, 1 | 2 | 4, 1 | 2 | 8, 2, 2 | 4, 2 | 4 | 8 };
            int mask = quadrants[c - '▖'];
            double hw = w / 2;
            double hh = h / 2;
            if ((mask & 1) != 0) { Rect(sb, x, y, hw, hh, fill); }
            if ((mask & 2) != 0) { Rect(sb, x + hw, y, w - hw, hh, fill); }
            if ((mask & 4) != 0) { Rect(sb, x, y + hh, hw, h - hh, fill); }
            if ((mask & 8) != 0) { Rect(sb, x + hw, y + hh, w - hw, h - hh, fill); }
            return true;
        }

        // Braille: dots 1-3 and 7 down the left column, 4-6 and 8 down the right, in a 2 by 4 grid.
        private static void Braille(StringBuilder sb, int bits, double x, double y, double w, double h, string fill) {
            if (bits == 0) {
                return;
            }
            int[] column = { 0, 0, 0, 1, 1, 1, 0, 1 };
            int[] row = { 0, 1, 2, 0, 1, 2, 3, 3 };
            double r = Math.Max(0.6, Math.Min(w * 0.14, h * 0.075));
            for (int dot = 0; dot < 8; dot++) {
                if ((bits & (1 << dot)) == 0) {
                    continue;
                }
                double dx = x + (w * (column[dot] == 0 ? 0.3 : 0.7));
                double dy = y + (h * ((row[dot] * 2) + 1) / 8.0);
                sb.Append("<circle cx=\"").Append(ExportText.Num(dx)).Append("\" cy=\"").Append(ExportText.Num(dy)).Append("\" r=\"").Append(ExportText.Num(r)).Append('"');
                ExportText.AppendPaint(sb, "fill", fill);
                sb.Append("/>");
            }
        }

        private static void Rect(StringBuilder sb, double x, double y, double w, double h, string fill, double opacity = 1) {
            if (w <= 0 || h <= 0) {
                return;
            }
            sb.Append("<rect x=\"").Append(ExportText.Num(x)).Append("\" y=\"").Append(ExportText.Num(y)).Append("\" width=\"").Append(ExportText.Num(w)).Append("\" height=\"").Append(ExportText.Num(h))
                .Append('"');
            ExportText.AppendPaint(sb, "fill", fill);
            if (opacity < 1) {
                sb.Append(" fill-opacity=\"").Append(ExportText.Num(opacity)).Append('"');
            }
            sb.Append("/>");
        }
    }
}
