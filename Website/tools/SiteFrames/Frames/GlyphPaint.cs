using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SiteFrames.Frames {
    /// <summary>
    /// CSS that paints box drawing (U+2500-257F), block elements (U+2580-259F) and braille (U+2801-28FF) on the cell grid.
    /// Fonts draw these characters at different weights and heights, and many leave gaps between rows; painting them as
    /// cell backgrounds makes borders join, bars meet and charts line up in every browser. The character itself stays in
    /// the text (with a transparent fill), so copying a frame gives the same text the terminal showed.
    /// Straight lines are gradient layers whose thickness follows <c>--cm-light</c> and <c>--cm-heavy</c>; arcs and
    /// diagonals are small SVG masks.
    /// </summary>
    internal static class GlyphPaint {
        // Light, heavy and double weights per direction (up, right, down, left) for U+2500-257F.
        // 0 none, 1 light, 2 heavy, 3 double; letters mark special shapes handled separately.
        private static readonly string[] Box = {
            "0101", "0202", "1010", "2020", "h1", "h2", "v1", "v2", "q1", "q2", "u1", "u2", "0110", "0210", "0120", "0220",
            "0011", "0012", "0021", "0022", "1100", "1200", "2100", "2200", "1001", "1002", "2001", "2002", "1110", "1210", "2110", "1120",
            "2120", "2210", "1220", "2220", "1011", "1012", "2011", "1021", "2021", "2012", "1022", "2022", "0111", "0112", "0211", "0212",
            "0121", "0122", "0221", "0222", "1101", "1102", "1201", "1202", "2101", "2102", "2201", "2202", "1111", "1112", "1211", "1212",
            "2111", "1121", "2121", "2112", "2211", "1122", "1221", "2212", "1222", "2122", "2221", "2222", "d1", "d2", "e1", "e2",
            "0303", "3030", "0310", "0130", "0330", "0013", "0031", "0033", "1300", "3100", "3300", "1003", "3001", "3003", "1310", "3130",
            "3330", "1013", "3031", "3033", "0313", "0131", "0333", "1303", "3101", "3303", "1313", "3131", "3333", "a0", "a1", "a2",
            "a3", "x0", "x1", "x2", "0001", "1000", "0100", "0010", "0002", "2000", "0200", "0020", "0201", "2010", "0102", "1020"
        };

        private const string Solid = "linear-gradient(currentColor,currentColor)";

        /// <summary>The line weights a box-drawing character has towards each edge (0 none, 1 light, 2 heavy, 3 double), or false for arcs, dashes and other shapes.</summary>
        public static bool TrySegments(char c, out int up, out int right, out int down, out int left) {
            up = right = down = left = 0;
            if (c < '\u2500' || c > '\u257F') {
                return false;
            }
            string code = Box[c - '\u2500'];
            if (!char.IsDigit(code[0])) {
                switch (code[0]) {
                    case 'h': case 'q': case 'd': right = left = 1; return true;
                    case 'v': case 'u': case 'e': up = down = 1; return true;
                    case 'a':
                        int which = code[1] - '0';
                        up = which >= 2 ? 1 : 0; down = which < 2 ? 1 : 0; right = which == 0 || which == 3 ? 1 : 0; left = which == 1 || which == 2 ? 1 : 0;
                        return true;
                    default: return false;
                }
            }
            up = code[0] - '0'; right = code[1] - '0'; down = code[2] - '0'; left = code[3] - '0';
            return true;
        }

        /// <summary>Whether the glyph paints the same across a run of cells, so a run can be one span (horizontal lines, full-width blocks and shades).</summary>
        public static bool Repeats(char c) {
            switch (c) {
                case '\u2500': case '\u2501': case '\u2550':
                case '\u2580': case '\u2588': case '\u2591': case '\u2592': case '\u2593': case '\u2594':
                    return true;
            }
            return c >= '\u2581' && c <= '\u2587';
        }

        public static bool Has(char c) {
            return (c >= '─' && c <= '▟') || (c > '⠀' && c <= '⣿');
        }

        public static string Rule(char c) {
            if (c >= '─' && c <= '╿') {
                return BoxRule(Box[c - '─']);
            }
            if (c >= '▀' && c <= '▟') {
                return BlockRule(c);
            }
            return BrailleRule(c - 0x2800);
        }

        private static string BoxRule(string code) {
            switch (code[0]) {
                case 'h': return Dashes(true, 3, code[1] == '2');
                case 'v': return Dashes(false, 3, code[1] == '2');
                case 'q': return Dashes(true, 4, code[1] == '2');
                case 'u': return Dashes(false, 4, code[1] == '2');
                case 'd': return Dashes(true, 2, code[1] == '2');
                case 'e': return Dashes(false, 2, code[1] == '2');
                case 'a': return Arc(code[1] - '0');
                case 'x': return Diagonal(code[1] - '0');
            }
            int up = code[0] - '0', right = code[1] - '0', down = code[2] - '0', left = code[3] - '0';
            List<string> layers = new List<string>();
            bool vDouble = up == 3 || down == 3;
            bool hDouble = left == 3 || right == 3;
            string vExtent = HalfExtent(Math.Max(up == 3 ? 0 : up, down == 3 ? 0 : down), vDouble, up != 0 || down != 0);
            string hExtent = HalfExtent(Math.Max(left == 3 ? 0 : left, right == 3 ? 0 : right), hDouble, left != 0 || right != 0);
            Horizontal(layers, right, true, up, down, vExtent);
            Horizontal(layers, left, false, up, down, vExtent);
            Vertical(layers, up, true, left, right, hExtent);
            Vertical(layers, down, false, left, right, hExtent);
            return "background-image:" + string.Join(",", Images(layers)) + ";background-position:" + string.Join(",", Positions(layers)) + ";background-size:" + string.Join(",", Sizes(layers));
        }

        // How far a joining line has to reach past the centre to cover the perpendicular strokes.
        private static string HalfExtent(int weight, bool isDouble, bool present) {
            if (!present) {
                return "0px";
            }
            if (isDouble) {
                return "calc(var(--cm-gap) + var(--cm-light) / 2)";
            }
            return weight == 2 ? "calc(var(--cm-heavy) / 2)" : "calc(var(--cm-light) / 2)";
        }

        private static string Thickness(int weight) => weight == 2 ? "var(--cm-heavy)" : "var(--cm-light)";

        private static void Horizontal(List<string> layers, int weight, bool toRight, int up, int down, string perpendicular) {
            if (weight == 0) {
                return;
            }
            string x = toRight ? "100%" : "0";
            if (weight != 3) {
                string reach = perpendicular == "0px" ? "calc(" + Thickness(weight) + " / 2)" : perpendicular;
                if (up == 3 || down == 3) {
                    reach = "calc(0px - var(--cm-gap) + var(--cm-light) / 2)";
                }
                layers.Add(x + " 50%|calc(50% + " + reach + ") " + Thickness(weight));
                return;
            }
            bool anyVerticalDouble = up == 3 || down == 3;
            foreach (int sign in new[] { -1, 1 }) {
                // Positive offsets are below the centre line.
                bool inner = (down == 3 && sign > 0) || (up == 3 && sign < 0);
                string reach = inner ? "calc(0px - var(--cm-gap) + var(--cm-light) / 2)" : anyVerticalDouble ? "calc(var(--cm-gap) + var(--cm-light) / 2)" : "calc(var(--cm-light) / 2)";
                if (!anyVerticalDouble && (up != 0 || down != 0)) {
                    reach = "calc(var(--cm-light) / 2)";
                }
                string y = sign < 0 ? "calc(50% - var(--cm-gap))" : "calc(50% + var(--cm-gap))";
                layers.Add(x + " " + y + "|calc(50% + " + reach + ") var(--cm-light)");
            }
        }

        private static void Vertical(List<string> layers, int weight, bool toTop, int left, int right, string perpendicular) {
            if (weight == 0) {
                return;
            }
            string y = toTop ? "0" : "100%";
            if (weight != 3) {
                string reach = perpendicular == "0px" ? "calc(" + Thickness(weight) + " / 2)" : perpendicular;
                if (left == 3 || right == 3) {
                    reach = "calc(0px - var(--cm-gap) + var(--cm-light) / 2)";
                }
                layers.Add("50% " + y + "|" + Thickness(weight) + " calc(50% + " + reach + ")");
                return;
            }
            bool anyHorizontalDouble = left == 3 || right == 3;
            foreach (int sign in new[] { -1, 1 }) {
                // Positive offsets are right of the centre line.
                bool inner = (right == 3 && sign > 0) || (left == 3 && sign < 0);
                string reach = inner ? "calc(0px - var(--cm-gap) + var(--cm-light) / 2)" : anyHorizontalDouble ? "calc(var(--cm-gap) + var(--cm-light) / 2)" : "calc(var(--cm-light) / 2)";
                if (!anyHorizontalDouble && (left != 0 || right != 0)) {
                    reach = "calc(var(--cm-light) / 2)";
                }
                string x = sign < 0 ? "calc(50% - var(--cm-gap))" : "calc(50% + var(--cm-gap))";
                layers.Add(x + " " + y + "|var(--cm-light) calc(50% + " + reach + ")");
            }
        }

        private static IEnumerable<string> Images(List<string> layers) {
            foreach (string _ in layers) {
                yield return Solid;
            }
        }

        private static IEnumerable<string> Positions(List<string> layers) {
            foreach (string layer in layers) {
                yield return layer.Substring(0, layer.IndexOf('|'));
            }
        }

        private static IEnumerable<string> Sizes(List<string> layers) {
            foreach (string layer in layers) {
                yield return layer.Substring(layer.IndexOf('|') + 1);
            }
        }

        private static string Dashes(bool horizontal, int count, bool heavy) {
            StringBuilder stops = new StringBuilder();
            double segment = 100.0 / count;
            for (int i = 0; i < count; i++) {
                double start = (i * segment) + (segment * 0.12);
                double end = start + (segment * 0.62);
                if (stops.Length > 0) {
                    stops.Append(',');
                }
                stops.Append("transparent ").Append(P(start)).Append(",currentColor ").Append(P(start)).Append(' ').Append(P(end)).Append(",transparent ").Append(P(end));
            }
            string t = heavy ? "var(--cm-heavy)" : "var(--cm-light)";
            string gradient = "linear-gradient(" + (horizontal ? "to right," : "to bottom,") + stops + ")";
            return "background-image:" + gradient + ";background-position:50% 50%;background-size:" + (horizontal ? "100% " + t : t + " 100%");
        }

        // Rounded corners: a quarter circle from the edge centre to the other edge centre, as a mask over currentColor.
        private static string Arc(int which) {
            // 0 ╭ down-right, 1 ╮ down-left, 2 ╯ up-left, 3 ╰ up-right; viewBox 10 x 20 with the centre at 5,10.
            string[] paths = {
                "M5 20V15A5 5 0 0 1 10 10",
                "M5 20V15A5 5 0 0 0 0 10",
                "M5 0V5A5 5 0 0 1 0 10",
                "M5 0V5A5 5 0 0 0 10 10"
            };
            return Mask(paths[which]);
        }

        private static string Diagonal(int which) {
            string[] paths = { "M10 0L0 20", "M0 0L10 20", "M10 0L0 20M0 0L10 20" };
            return Mask(paths[which]);
        }

        private static string Mask(string path) {
            string svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 20' preserveAspectRatio='none'><path d='" + path
                + "' fill='none' stroke='black' stroke-width='1.25' vector-effect='non-scaling-stroke'/></svg>";
            string url = "url(\"data:image/svg+xml," + Uri.EscapeDataString(svg).Replace("%20", " ").Replace("%27", "'").Replace("%2F", "/").Replace("%3D", "=").Replace("%3A", ":") + "\")";
            return "background-color:currentColor;-webkit-mask:" + url + " 0 0/100% 100% no-repeat;mask:" + url + " 0 0/100% 100% no-repeat";
        }

        private static string BlockRule(char c) {
            switch (c) {
                case '▀': return "background-image:linear-gradient(currentColor 50%,transparent 0)";
                case '█': return "background-color:currentColor";
                case '▐': return "background-image:linear-gradient(to left,currentColor 50%,transparent 0)";
                case '░': return "background-color:color-mix(in srgb,currentColor 25%,transparent)";
                case '▒': return "background-color:color-mix(in srgb,currentColor 50%,transparent)";
                case '▓': return "background-color:color-mix(in srgb,currentColor 75%,transparent)";
                case '▔': return "background-image:linear-gradient(currentColor 12.5%,transparent 0)";
                case '▕': return "background-image:linear-gradient(to left,currentColor 12.5%,transparent 0)";
            }
            if (c >= '▁' && c <= '▇') {
                return "background-image:linear-gradient(to top,currentColor " + P((c - '▀') * 12.5) + ",transparent 0)";
            }
            if (c >= '▉' && c <= '▏') {
                return "background-image:linear-gradient(to right,currentColor " + P(('▐' - c) * 12.5) + ",transparent 0)";
            }
            // Quadrants U+2596-259F: bits 1 upper left, 2 upper right, 4 lower left, 8 lower right.
            int[] quadrants = { 4, 8, 1, 1 | 4 | 8, 1 | 8, 1 | 2 | 4, 1 | 2 | 8, 2, 2 | 4, 2 | 4 | 8 };
            int mask = quadrants[c - '▖'];
            List<string> images = new List<string>();
            List<string> positions = new List<string>();
            if ((mask & 1) != 0) { images.Add(Solid); positions.Add("0 0"); }
            if ((mask & 2) != 0) { images.Add(Solid); positions.Add("100% 0"); }
            if ((mask & 4) != 0) { images.Add(Solid); positions.Add("0 100%"); }
            if ((mask & 8) != 0) { images.Add(Solid); positions.Add("100% 100%"); }
            string[] sizes = new string[images.Count];
            for (int i = 0; i < sizes.Length; i++) {
                sizes[i] = "50% 50%";
            }
            return "background-image:" + string.Join(",", images) + ";background-position:" + string.Join(",", positions) + ";background-size:" + string.Join(",", sizes);
        }

        // Dots 1-3 and 7 run down the left column, 4-6 and 8 down the right, on a 2 x 4 grid.
        private static string BrailleRule(int bits) {
            int[] column = { 0, 0, 0, 1, 1, 1, 0, 1 };
            int[] row = { 0, 1, 2, 0, 1, 2, 3, 3 };
            List<string> dots = new List<string>();
            for (int dot = 0; dot < 8; dot++) {
                if ((bits & (1 << dot)) == 0) {
                    continue;
                }
                string x = column[dot] == 0 ? "28%" : "72%";
                string y = P(((row[dot] * 2) + 1) * 12.5);
                dots.Add("radial-gradient(circle at " + x + " " + y + ",currentColor var(--cm-dot),transparent calc(var(--cm-dot) + .6px))");
            }
            return "background-image:" + string.Join(",", dots);
        }

        private static string P(double percent) => percent.ToString("0.###", CultureInfo.InvariantCulture) + "%";
    }
}
