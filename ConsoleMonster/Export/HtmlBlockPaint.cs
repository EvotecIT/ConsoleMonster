using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>One SVG coordinate space for a screen's blocks, so all touching edges snap to the same device pixels.</summary>
    internal static class HtmlBlockPaint {
        private static readonly string Template = ConsoleExporter.ReadAsset("screen-blocks.svg").TrimEnd('\r', '\n');

        public static void AppendFrame(StringBuilder html, CellBuffer frame, ExportPalette palette) {
            SvgShapes shapes = new SvgShapes();
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    if (cell.Glyph.Length != 1 || cell.Glyph[0] < '\u2580' || cell.Glyph[0] > '\u259F') {
                        continue;
                    }
                    int count = 1;
                    if (SvgGlyphs.Stretches(cell.Glyph)) {
                        while (x + count < frame.Width && frame[x + count, y].Glyph == cell.Glyph && frame[x + count, y].Style.Equals(cell.Style)) {
                            count++;
                        }
                    }
                    palette.Resolve(cell.Style, out string fill, out _, out _);
                    shapes.Opacity = ExportPalette.ForegroundOpacity(cell.Style, fill);
                    SvgGlyphs.Append(shapes, cell.Glyph, x * 8, y * 16, count * 8, 16, fill);
                    x += count - 1;
                }
            }
            if (shapes.IsEmpty) {
                return;
            }
            StringBuilder paths = new StringBuilder();
            shapes.AppendTo(paths);
            html.Append(Template.Replace("__WIDTH__", (frame.Width * 8).ToString(CultureInfo.InvariantCulture))
                .Replace("__HEIGHT__", (frame.Height * 16).ToString(CultureInfo.InvariantCulture))
                .Replace("__SHAPES__", paths.ToString()));
        }
    }
}
