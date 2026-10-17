using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace SiteFrames.Frames {
    /// <summary>
    /// A miniature of a frame as SVG for navigation thumbnails: backgrounds and block elements as they are, box-drawing
    /// lines as hairlines, and each run of text as a bar in its colour, so the shape of the real output reads at 96 px.
    /// </summary>
    internal static class FrameMinimap {
        private const double CellW = 6;
        private const double CellH = 12;

        public static string Svg(CellBuffer frame, ColorDepth depth, Screen screen, int columns, int rows) {
            int w = Math.Min(columns, frame.Width);
            int h = Math.Min(rows, frame.Height);
            StringBuilder sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ").Append(N(w * CellW + 12)).Append(' ').Append(N(h * CellH + 12))
                .Append("\" preserveAspectRatio=\"xMinYMin slice\"><rect width=\"100%\" height=\"100%\" fill=\"").Append(screen.Background).Append("\"/><g transform=\"translate(6 6)\">");
            for (int y = 0; y < h; y++) {
                int x = 0;
                while (x < w) {
                    Cell cell = frame[x, y];
                    Style style = FrameHtml.Reduce(cell.Style, depth);
                    string fg = Color(style.Foreground, screen.Foreground, style);
                    if (!style.Background.IsDefault) {
                        int end = x + 1;
                        while (end < w && FrameHtml.Reduce(frame[end, y].Style, depth).Background.Equals(style.Background)) {
                            end++;
                        }
                        Rect(sb, x * CellW, y * CellH, (end - x) * CellW, CellH, Color(style.Background, screen.Background, Style.Plain), 1);
                    }
                    string glyph = cell.Glyph ?? " ";
                    char c = glyph.Length == 1 ? glyph[0] : '\0';
                    if (cell.IsContinuation || glyph == " " || glyph.Length == 0 || c == '⠀') {
                        x++;
                        continue;
                    }
                    if (GlyphPaint.TrySegments(c, out int up, out int right, out int down, out int left)) {
                        double cx = x * CellW + CellW / 2, cy = y * CellH + CellH / 2, t = 0.9;
                        if (left > 0) { Rect(sb, x * CellW, cy - t / 2, CellW / 2 + t / 2, t, fg, .9); }
                        if (right > 0) { Rect(sb, cx - t / 2, cy - t / 2, CellW / 2 + t / 2, t, fg, .9); }
                        if (up > 0) { Rect(sb, cx - t / 2, y * CellH, t, CellH / 2 + t / 2, fg, .9); }
                        if (down > 0) { Rect(sb, cx - t / 2, cy - t / 2, t, CellH / 2 + t / 2, fg, .9); }
                        x++;
                        continue;
                    }
                    if (c >= '▁' && c <= '█') {
                        double part = (c - '▀') / 8.0;
                        Rect(sb, x * CellW, y * CellH + CellH * (1 - part), CellW, CellH * part, fg, 1);
                        x++;
                        continue;
                    }
                    if (c >= '▉' && c <= '▏') {
                        Rect(sb, x * CellW, y * CellH, CellW * ('▐' - c) / 8.0, CellH, fg, 1);
                        x++;
                        continue;
                    }
                    if (c > '⠀' && c <= '⣿') {
                        Rect(sb, x * CellW + 1, y * CellH + 3, CellW - 2, CellH - 6, fg, .55);
                        x++;
                        continue;
                    }
                    // A run of text in one colour becomes one bar.
                    int runEnd = x + 1;
                    while (runEnd < w) {
                        Cell next = frame[runEnd, y];
                        string ng = next.Glyph ?? " ";
                        if (next.IsContinuation) { runEnd++; continue; }
                        if (ng == " " && runEnd + 1 < w && (frame[runEnd + 1, y].Glyph ?? " ") != " " && !IsShape(frame[runEnd + 1, y].Glyph)) {
                            Style ns = FrameHtml.Reduce(frame[runEnd + 1, y].Style, depth);
                            if (Color(ns.Foreground, screen.Foreground, ns) == fg) { runEnd++; continue; }
                        }
                        if (ng == " " || IsShape(ng)) { break; }
                        Style s2 = FrameHtml.Reduce(next.Style, depth);
                        if (Color(s2.Foreground, screen.Foreground, s2) != fg) { break; }
                        runEnd++;
                    }
                    bool bold = (style.Decoration & TextDecoration.Bold) != 0;
                    Rect(sb, x * CellW + .6, y * CellH + (bold ? 3.2 : 3.8), (runEnd - x) * CellW - 1.2, bold ? 5.6 : 4.4, fg, bold ? .95 : .7);
                    x = runEnd;
                }
            }
            Flush(sb);
            sb.Append("</g></svg>\n");
            return sb.ToString();
        }

        private static bool IsShape(string? glyph) {
            if (string.IsNullOrEmpty(glyph) || glyph.Length != 1) {
                return false;
            }
            return GlyphPaint.Has(glyph[0]);
        }

        private static string Color(ConsoleMonster.Styling.Color color, string fallback, Style style) {
            if ((style.Decoration & TextDecoration.Reverse) != 0) {
                return fallback;
            }
            if (color.IsDefault) {
                return fallback;
            }
            color.ToRgb(out byte r, out byte g, out byte b);
            return "#" + FrameHtml.Hex(r, g, b);
        }

        // Rectangles are collected per colour and opacity and written as one path each, which keeps a thumbnail small.
        [ThreadStatic] private static Dictionary<string, StringBuilder>? _paths;

        private static void Rect(StringBuilder sb, double x, double y, double w, double h, string fill, double opacity) {
            _paths ??= new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
            string key = fill + "|" + N(opacity);
            if (!_paths.TryGetValue(key, out StringBuilder? d)) {
                d = new StringBuilder();
                _paths[key] = d;
            }
            d.Append('M').Append(N(x)).Append(' ').Append(N(y)).Append('h').Append(N(w)).Append('v').Append(N(h)).Append('h').Append(N(-w)).Append('z');
        }

        private static void Flush(StringBuilder sb) {
            if (_paths == null) {
                return;
            }
            List<string> keys = new List<string>(_paths.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string key in keys) {
                string[] parts = key.Split('|');
                sb.Append("<path fill=\"").Append(parts[0]).Append('"');
                if (parts[1] != "1") {
                    sb.Append(" opacity=\"").Append(parts[1]).Append('"');
                }
                sb.Append(" d=\"").Append(_paths[key]).Append("\"/>");
            }
            _paths.Clear();
        }

        private static string N(double v) => Math.Round(v, 1).ToString("0.#", CultureInfo.InvariantCulture);
    }
}
