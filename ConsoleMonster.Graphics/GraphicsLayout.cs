using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;

namespace ConsoleMonster.Graphics {
    // One layout is prepared per still or clip. All encoders receive the same composed RGBA canvas.
    internal sealed class GraphicsLayout {
        internal int Width { get; }
        internal int Height { get; }
        internal int Columns { get; }
        internal int Rows { get; }
        internal double X { get; }
        internal double Y { get; }
        internal ExportFrameSize Window { get; }
        internal ExportOptions Frame { get; }
        internal GraphicsLayout(int columns, int rows, GraphicsExportOptions options, ExportOptions frame) {
            Columns = Math.Max(1, columns); Rows = Math.Max(1, rows); Frame = frame;
            switch (options.Preset) {
                case GraphicsPreset.Slide16x9: Width = 1920; Height = 1080; break;
                case GraphicsPreset.Slide4x3: Width = 1440; Height = 1080; break;
                case GraphicsPreset.Social: Width = 1200; Height = 630; break;
            }
            if (Width > 0) {
                double inset = Math.Ceiling(Math.Min(Width, Height) * 0.06);
                Frame.Padding = (int)Math.Round(Math.Min(Width, Height) * 0.022);
                Frame.FontSize = 4;
                if (!Fits()) { throw new ArgumentException("The terminal grid is too large for this slide preset. Record fewer columns or rows."); }
                double low = 4, high = options.MaxFontSize;
                for (int i = 0; i < 24; i++) {
                    Frame.FontSize = (low + high) / 2;
                    if (Fits()) { low = Frame.FontSize; } else { high = Frame.FontSize; }
                }
                Frame.FontSize = Math.Floor(low * 100) / 100;
                bool Fits() {
                    ExportFrameSize size = ConsoleExporter.MeasureFrame(Columns, Rows, Frame);
                    return size.Width <= Width - 2 * inset && size.Height <= Height - 2 * inset;
                }
            }
            Window = ConsoleExporter.MeasureFrame(Columns, Rows, Frame);
            X = (Width - Window.Width) / 2; Y = (Height - Window.Height) / 2;
        }
        internal CellBuffer Grid(CellBuffer source) {
            if (source.Width == Columns && source.Height == Rows) { return source; }
            CellBuffer result = new CellBuffer(Columns, Rows);
            result.Blit(source, 0, 0, Math.Min(source.Width, Columns), Math.Min(source.Height, Rows), 0, 0);
            return result;
        }
    }
}
