using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;

namespace ConsoleMonster.Graphics {
    // The core SVG writer is the canonical geometric cell painter (boxes, blocks, braille and chrome).
    // ChartForgeX resolves its text and rasterises those shapes without a browser or native dependency.
    internal static class GraphicsRenderer {
        private const string EmbeddedFamily = "ConsoleMonster Embedded JetBrains Mono";
        private static readonly Lazy<bool> Font = new Lazy<bool>(() => {
            using (Stream stream = typeof(GraphicsRenderer).Assembly.GetManifestResourceStream("ConsoleMonster.Graphics.JetBrainsMono.ttf")!) {
                FontRegistry.Register(EmbeddedFamily, stream);
            }
            return true;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        internal static GraphicsLayout Layout(CellBuffer frame, GraphicsExportOptions options, int? columns = null, int? rows = null) {
            options.Validate();
            _ = Font.Value;
            ExportOptions source = options.Frame;
            ExportOptions render = new ExportOptions {
                Theme = source.Theme == ExportTheme.Auto ? ExportTheme.Dark : source.Theme, DarkTheme = source.DarkTheme,
                LightTheme = source.LightTheme, Width = frame.Width, Height = frame.Height,
                Title = source.Title, WindowChrome = source.WindowChrome, ChromeStyle = source.ChromeStyle,
                FontSize = source.FontSize, LineHeight = source.LineHeight, Padding = source.Padding,
                Glyphs = source.Glyphs, Culture = source.Culture,
                FontFamily = FontStack(options), IndividualTextElements = true
            };
            int height = rows ?? (options.Preset != GraphicsPreset.Docs && options.Fit == GraphicsFit.Content ? ConsoleExporter.MeasureContentRows(frame) : frame.Height);
            return new GraphicsLayout(columns ?? ContentColumns(frame, options), height, options, render);
        }
        // Keep two trailing cells for breathing room without growing beyond the source terminal.
        internal static int ContentColumns(CellBuffer frame, GraphicsExportOptions options) =>
            options.Preset != GraphicsPreset.Docs && options.Fit == GraphicsFit.Content
                ? Math.Min(frame.Width, ConsoleExporter.MeasureContentColumns(frame) + 2) : frame.Width;
        private static string Svg(CellBuffer frame, GraphicsExportOptions options, GraphicsLayout layout) {
            ExportOptions ready = ConsoleExporter.Ready(layout.Frame);
            // hhea.ascender / unitsPerEm for the embedded regular face. Other font stacks keep the SVG baseline.
            double? ascent = options.FontFamily == null ? layout.Frame.FontSize * 1.02 : (double?)null;
            XElement window = XElement.Parse(SvgFrameWriter.Write(ConsoleExporter.Layers(layout.Grid(frame), ready), ready, raster: true, ascent: ascent));
            XNamespace ns = window.Name.Namespace;
            window.Add(new XElement(ns + "style", ".cm-title{font-family:" + layout.Frame.FontFamily + ";font-size:13px;font-weight:400}"));
            return (layout.Width == 0 ? window : GraphicsComposition.Compose(window, layout, options)).ToString(SaveOptions.DisableFormatting);
        }
        internal static RgbaImage Render(CellBuffer frame, GraphicsExportOptions options) => Render(frame, options, Layout(frame, options));
        internal static RgbaImage Render(CellBuffer frame, GraphicsExportOptions options, GraphicsLayout layout) => SvgRasterizer.ToImage(Svg(frame, options, layout));
        internal static byte[] Png(CellBuffer frame, GraphicsExportOptions options) => Render(frame, options).ToPng();
        private static string FontStack(GraphicsExportOptions options) {
            string stack = options.FontFamily == null ? "'" + EmbeddedFamily + "'" : Quote(options.FontFamily) + ", '" + EmbeddedFamily + "'";
            foreach (string family in options.FontFallbacks) { stack += ", " + Quote(family); }
            return stack + ", 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', 'Noto Sans CJK SC', 'PingFang SC', 'Microsoft YaHei', monospace";
        }
        private static string Quote(string family) {
            if (string.IsNullOrWhiteSpace(family) || family.IndexOfAny(new[] { '\'', '"', ';', '{', '}', '<', '>', '\\', '\r', '\n' }) >= 0) { throw new ArgumentException("Use an installed font family name, without CSS syntax."); }
            return "'" + family + "'";
        }
    }
}
