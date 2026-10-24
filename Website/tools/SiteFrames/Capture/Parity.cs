using VtScreen = ConsoleMonster.Sessions.VtScreen;
using System;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Frames;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>The result of comparing what a PowerShell host drew with the frame rendered in-process.</summary>
    internal sealed class ParityResult {
        public ParityResult(bool identical, string detail) {
            Identical = identical;
            Detail = detail;
        }

        public bool Identical { get; }

        public string Detail { get; }
    }

    /// <summary>Compares two frames cell by cell: the glyph and the style the terminal received.</summary>
    internal static class Parity {
        /// <summary>Replays a saved VT stream into a screen of the given width and returns the printed rows.</summary>
        public static CellBuffer Transcript(byte[] vt, int width, int height) {
            VtScreen screen = new VtScreen(width, height);
            screen.Feed(vt, vt.Length);
            return Trim(screen.Transcript());
        }

        public static CellBuffer Trim(CellBuffer frame) {
            int last = frame.Height - 1;
            while (last > 0 && Blank(frame, last)) {
                last--;
            }
            CellBuffer trimmed = new CellBuffer(frame.Width, last + 1);
            trimmed.Blit(frame, 0, 0, frame.Width, last + 1, 0, 0);
            return trimmed;
        }

        private static bool Blank(CellBuffer frame, int y) => FrameHtml.ContentEnd(frame, y, ColorDepth.TrueColor) == 0;

        public static ParityResult Compare(CellBuffer expected, CellBuffer actual) {
            CellBuffer a = Trim(expected);
            CellBuffer b = Trim(actual);
            if (a.Width != b.Width || a.Height != b.Height) {
                return new ParityResult(false, "frame dimensions differ");
            }
            int rows = Math.Max(a.Height, b.Height);
            for (int y = 0; y < rows; y++) {
                string left = y < a.Height ? a.GetRowText(y).TrimEnd() : string.Empty;
                string right = y < b.Height ? b.GetRowText(y).TrimEnd() : string.Empty;
                if (!string.Equals(left, right, StringComparison.Ordinal)) {
                    return new ParityResult(false, "row " + (y + 1) + " differs: expected \"" + left + "\", got \"" + right + "\"");
                }
                int width = Math.Min(a.Width, b.Width);
                for (int x = 0; x < width && y < a.Height && y < b.Height; x++) {
                    Cell ca = a[x, y];
                    Cell cb = b[x, y];
                    if (ca.Width != cb.Width || !string.Equals(ca.Glyph, cb.Glyph, StringComparison.Ordinal)) {
                        return new ParityResult(false, "row " + (y + 1) + ", column " + (x + 1) + " cell geometry differs");
                    }
                    if (ca.IsContinuation || (ca.Glyph == " " && cb.Glyph == " " && ca.Style.BackgroundColor.IsDefault && cb.Style.BackgroundColor.IsDefault && ca.Style.Decoration == cb.Style.Decoration)) {
                        continue;
                    }
                    Style sa = Normal(ca.Style);
                    Style sb = Normal(cb.Style);
                    if (!sa.Equals(sb)) {
                        return new ParityResult(false, "row " + (y + 1) + ", column " + (x + 1) + " style differs: expected " + sa + ", got " + sb);
                    }
                }
            }
            return new ParityResult(true, "identical");
        }

        // The terminal receives RGB for theme colours; compare by value whatever the colour kind.
        private static Style Normal(Style style) {
            Style s = FrameHtml.Reduce(style, ColorDepth.TrueColor);
            return new Style(Rgb(s.Color), Rgb(s.BackgroundColor), s.Decoration, s.Link);
        }

        private static Color Rgb(Color color) {
            if (color.IsDefault) {
                return color;
            }
            color.ToRgb(out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }

        public static string Describe(CellBuffer frame) {
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < frame.Height; y++) {
                sb.Append(frame.GetRowText(y).TrimEnd()).Append('\n');
            }
            return sb.ToString();
        }
    }
}
