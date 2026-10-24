using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
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
        internal static string Svg(CellBuffer frame, GraphicsExportOptions options) {
            options.Validate();
            _ = Font.Value;
            ExportOptions source = options.Frame;
            ExportOptions render = new ExportOptions {
                Theme = source.Theme == ExportTheme.Auto ? ExportTheme.Dark : source.Theme, DarkTheme = source.DarkTheme,
                LightTheme = source.LightTheme, Width = frame.Width, Height = frame.Height,
                Title = source.Title, WindowChrome = source.WindowChrome, ChromeStyle = source.ChromeStyle,
                FontSize = source.FontSize, LineHeight = source.LineHeight, Padding = source.Padding,
                FontFamily = FontStack(options), IndividualTextElements = true
            };
            int width = 0, height = 0;
            switch (options.Preset) {
                case GraphicsPreset.Slide16x9: width = 1920; height = 1080; break;
                case GraphicsPreset.Slide4x3: width = 1440; height = 1080; break;
                case GraphicsPreset.Social: width = 1200; height = 630; break;
            }
            if (width > 0) {
                render.Padding = 32;
                double chrome = !render.WindowChrome || render.ChromeStyle == WindowChromeStyle.None ? 0 : render.ChromeStyle == WindowChromeStyle.MacOS ? 28 : render.ChromeStyle == WindowChromeStyle.Windows ? 32 : 36;
                double preferred = source.FontSize == 14 ? 36 : source.FontSize;
                render.FontSize = Math.Min(preferred, Math.Min((width - 96.0) / (frame.Width * 0.6), (height - 96.0 - chrome) / (frame.Height * render.LineHeight)));
                if (render.FontSize < 4) { throw new ArgumentException("The terminal grid is too large for this slide preset. Record fewer columns or rows."); }
            }
            XElement root = XElement.Parse(ConsoleExporter.ToSvg(frame, render));
            XNamespace ns = root.Name.Namespace;
            root.Add(new XElement(ns + "style", ".cm-title{font-family:" + render.FontFamily + "}"));
            if (width == 0) { return root.ToString(SaveOptions.DisableFormatting); }
            double naturalWidth = double.Parse(root.Attribute("width")!.Value, CultureInfo.InvariantCulture);
            double naturalHeight = double.Parse(root.Attribute("height")!.Value, CultureInfo.InvariantCulture);
            XElement group = new XElement(ns + "g", new XAttribute("transform", "translate(" + N((width - naturalWidth) / 2) + " " + N((height - naturalHeight) / 2) + ")"), root.Nodes().ToArray());
            root.RemoveNodes();
            root.SetAttributeValue("width", width); root.SetAttributeValue("height", height); root.SetAttributeValue("viewBox", "0 0 " + width + " " + height);
            root.Add(new XElement(ns + "rect", new XAttribute("width", width), new XAttribute("height", height), new XAttribute("fill", render.Theme == ExportTheme.Light ? "#ffffff" : render.Theme == ExportTheme.HighContrast ? "#000000" : "#0c0c0c")), group);
            return root.ToString(SaveOptions.DisableFormatting);
        }
        internal static RgbaImage Render(CellBuffer frame, GraphicsExportOptions options) => SvgRasterizer.ToImage(Svg(frame, options));
        internal static byte[] Png(CellBuffer frame, GraphicsExportOptions options) => SvgRasterizer.ToPng(Svg(frame, options));
        private static string FontStack(GraphicsExportOptions options) {
            string stack = options.FontFamily == null ? "'" + EmbeddedFamily + "'" : Quote(options.FontFamily) + ", '" + EmbeddedFamily + "'";
            foreach (string family in options.FontFallbacks) { stack += ", " + Quote(family); }
            return stack + ", 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', 'Noto Sans CJK SC', 'PingFang SC', 'Microsoft YaHei', monospace";
        }
        private static string Quote(string family) {
            if (string.IsNullOrWhiteSpace(family) || family.IndexOfAny(new[] { '\'', '"', ';', '{', '}', '<', '>', '\\', '\r', '\n' }) >= 0) { throw new ArgumentException("Use an installed font family name, without CSS syntax."); }
            return "'" + family + "'";
        }
        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
