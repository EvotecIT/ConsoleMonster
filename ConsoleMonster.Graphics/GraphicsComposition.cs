using System;
using System.Globalization;
using System.Xml.Linq;
using ConsoleMonster.Export;

namespace ConsoleMonster.Graphics {
    // Slide-specific presentation stays here; ChartForgeX owns SVG shapes, gradients, clipping and rasterisation.
    internal static class GraphicsComposition {
        internal static XElement Compose(XElement window, GraphicsLayout layout, GraphicsExportOptions options) {
            XNamespace ns = window.Name.Namespace;
            string theme = layout.Frame.Theme == ExportTheme.Light ? "#ffffff" : layout.Frame.Theme == ExportTheme.HighContrast ? "#000000" : "#0c0c0c";
            string fill = options.Background == GraphicsBackground.Solid ? options.BackgroundColor! : theme;
            XElement defs = new XElement(ns + "defs", new XElement(ns + "clipPath", new XAttribute("id", "cm-window-clip"), new XAttribute("clipPathUnits", "userSpaceOnUse"),
                Rect(0, 0, layout.Window.Width, layout.Window.Height, "#ffffff", 14)));
            if (options.Background == GraphicsBackground.Gradient) {
                string start = options.BackgroundColor ?? (layout.Frame.Theme == ExportTheme.Light ? "#e5ecf7" : layout.Frame.Theme == ExportTheme.HighContrast ? "#171717" : "#192332");
                defs.Add(new XElement(ns + "linearGradient", new XAttribute("id", "cm-slide-gradient"), new XAttribute("x1", "0%"), new XAttribute("y1", "0%"), new XAttribute("x2", "100%"), new XAttribute("y2", "100%"),
                    new XElement(ns + "stop", new XAttribute("offset", "0%"), new XAttribute("stop-color", start)),
                    new XElement(ns + "stop", new XAttribute("offset", "100%"), new XAttribute("stop-color", options.BackgroundEndColor ?? theme))));
                fill = "url(#cm-slide-gradient)";
            }
            XElement slide = new XElement(ns + "svg", new XAttribute("width", layout.Width), new XAttribute("height", layout.Height), new XAttribute("viewBox", "0 0 " + layout.Width + " " + layout.Height), defs,
                Rect(0, 0, layout.Width, layout.Height, fill, 0));
            // Layered soft outlines give a small shadow without an SVG filter or an external blur engine.
            foreach (int spread in new[] { 24, 16, 8 }) {
                XElement shadow = Rect(layout.X, layout.Y + 5, layout.Window.Width, layout.Window.Height, "none", 14);
                shadow.SetAttributeValue("stroke", "#000000"); shadow.SetAttributeValue("stroke-width", spread);
                shadow.SetAttributeValue("stroke-opacity", spread == 24 ? "0.025" : spread == 16 ? "0.035" : "0.05");
                slide.Add(shadow);
            }
            // Retain the terminal's own viewport: its text metrics and nested chrome use that coordinate space.
            slide.Add(new XElement(ns + "g", new XAttribute("transform", "translate(" + N(layout.X) + " " + N(layout.Y) + ")"),
                new XAttribute("clip-path", "url(#cm-window-clip)"), window));
            XElement border = Rect(layout.X + 0.5, layout.Y + 0.5, layout.Window.Width - 1, layout.Window.Height - 1, "none", 14);
            border.SetAttributeValue("stroke", layout.Frame.Theme == ExportTheme.Light ? "#000000" : "#ffffff");
            border.SetAttributeValue("stroke-opacity", layout.Frame.Theme == ExportTheme.HighContrast ? "0.35" : "0.13");
            border.SetAttributeValue("stroke-width", 1);
            slide.Add(border);
            return slide;
            XElement Rect(double x, double y, double width, double height, string paint, double radius) => new XElement(ns + "rect",
                new XAttribute("x", N(x)), new XAttribute("y", N(y)), new XAttribute("width", N(width)), new XAttribute("height", N(height)), new XAttribute("rx", N(radius)), new XAttribute("fill", paint));
        }
        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
