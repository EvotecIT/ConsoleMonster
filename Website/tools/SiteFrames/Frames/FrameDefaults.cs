using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace SiteFrames.Frames {
    /// <summary>Interprets terminal defaults under an explicitly declared site palette.</summary>
    internal static class FrameDefaults {
        internal static CellBuffer Resolve(CellBuffer frame, Screen screen) => Map(frame, screen, false);
        internal static CellBuffer ForRecording(CellBuffer frame, Screen screen) => Map(frame, screen, true);

        private static CellBuffer Map(CellBuffer frame, Screen screen, bool recording) {
            var mapped = new CellBuffer(frame.Width, frame.Height);
            Color defaultForeground = Color.FromHex(screen.Foreground), defaultBackground = Color.FromHex(screen.Background);
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    if (cell.IsContinuation) { continue; }
                    Style style = cell.Style;
                    Color foreground = style.Color.IsDefault ? defaultForeground : Rgb(style.Color);
                    Color background = style.BackgroundColor.IsDefault ? defaultBackground : Rgb(style.BackgroundColor);
                    if (recording) {
                        if (foreground.Equals(defaultForeground) || cell.Glyph == " " && cell.Width == 1 &&
                            (style.Decoration & (TextDecoration.Underline | TextDecoration.Strikethrough | TextDecoration.Reverse)) == 0) { foreground = Color.Default; }
                        if (background.Equals(defaultBackground)) { background = Color.Default; }
                    }
                    mapped.Set(x, y, cell.Glyph, new Style(foreground, background, style.Decoration, style.Link), cell.Width);
                }
            }
            return mapped;
        }

        private static Color Rgb(Color color) {
            color.ToRgb(out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }
    }
}
