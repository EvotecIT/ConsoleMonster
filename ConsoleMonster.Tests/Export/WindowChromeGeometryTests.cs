using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    public sealed class WindowChromeGeometryTests {
        [Theory]
        [InlineData(WindowChromeStyle.MacOS, 28, 10)]
        [InlineData(WindowChromeStyle.Windows, 32, 8)]
        [InlineData(WindowChromeStyle.Linux, 36, 12)]
        public void MeasuredWindowIncludesOnePageInsetAndClipsEveryBodyLayer(WindowChromeStyle style, int bar, int radius) {
            var frame = new CellBuffer(75, 8);
            frame.Write(0, 0, "Ready", Style.Plain);
            var options = new ExportOptions { WindowChrome = true, ChromeStyle = style, Theme = ExportTheme.Auto, Title = "Terminal export" };
            var size = ConsoleExporter.MeasureFrame(frame.Width, frame.Height, options);
            XElement root = XElement.Parse(ConsoleExporter.ToSvg(frame, options));
            Assert.Equal(670, size.Width);
            Assert.Equal(Math.Ceiling(bar + 40 + 8 * 16.8), size.Height);
            Assert.Equal(size.Width, N(root, "width"));
            Assert.Equal(size.Height, N(root, "height"));
            XElement inner = root.Descendants().Single(e => (string?)e.Attribute("id") == "cm-inner-clip").Elements().Single();
            Assert.Equal(9, N(inner, "x")); Assert.Equal(9, N(inner, "y"));
            Assert.Equal(radius - 1, N(inner, "rx"));
            Assert.Equal(size.Width - 18, N(inner, "width")); Assert.Equal(size.Height - 18, N(inner, "height"));
            XElement[] layers = root.Descendants().Where(e => ((string?)e.Attribute("class"))?.StartsWith("cm-layer", StringComparison.Ordinal) == true).ToArray();
            Assert.Single(layers);
            foreach (XElement layer in layers) {
                Assert.Equal("url(#cm-inner-clip)", (string?)layer.Parent!.Attribute("clip-path"));
                Assert.Equal("translate(20 " + (bar + 20) + ")", (string?)layer.Attribute("transform"));
                Assert.Equal(14, N(layer.Elements().First(e => e.Name.LocalName == "text"), "y"));
            }
            Assert.Contains("stdDeviation=\"18\"", root.ToString());
            Assert.Contains("font-size:13px;font-weight:400", root.Value);
        }

        [Fact]
        public void WindowsTabUsesShellTileAndIndependentTabAndWindowControls() {
            var options = new ExportOptions { WindowChrome = true, ChromeStyle = WindowChromeStyle.Windows, Theme = ExportTheme.Dark, Title = "PowerShell" };
            string svg = ConsoleExporter.ToSvg(new CellBuffer(75, 8), options);
            Assert.Contains("x=\"16\" y=\"12\" width=\"300\" height=\"28\" rx=\"6\"", svg);
            Assert.Contains("translate(28 17)", svg);
            Assert.Contains("M4 5l4 4-4 4M9.5 13h5", svg);
            Assert.Contains("M293 22l8 8m-8 0l8 -8", svg);
            Assert.Contains("translate(316 0)", svg);
            Assert.Contains("M639 20l12 12m-12 0l12 -12", svg);
            string html = ConsoleExporter.ToHtml(new CellBuffer(75, 8), options);
            Assert.Contains("class=\"cm-shell\"", html); Assert.Contains("class=\"cm-tab-close\"", html);
            Assert.Contains("class=\"cm-tab-actions\"", html); Assert.Contains("class=\"cm-right\"", html);
            Assert.Contains("padding:8px;background:var(--cm-desk)", html);
            Assert.Contains("stdDeviation=\"18\"", html);
            Assert.Contains("--cm-chrome-title:#cccccc", html);
        }

        [Fact]
        public void NarrowWindowsExportsReserveSpaceForMenuAndWindowControls() {
            var options = new ExportOptions { WindowChrome = true, ChromeStyle = WindowChromeStyle.Windows, FontSize = 4, Padding = 0 };
            XElement root = XElement.Parse(ConsoleExporter.ToSvg(new CellBuffer(1, 1), options));
            double width = N(root, "width");
            double tabWidth = Math.Min(300, Math.Max(72, Math.Max(96, width - 230)));
            Assert.True(16 + tabWidth + 60 + 8 <= width - 106);
            Assert.Contains("min-width:272px", ConsoleExporter.ToHtml(new CellBuffer(1, 1), options));
        }

        [Fact]
        public void NativeSvgUsesPaintedShadowAndEmbeddedFontAscentWithoutMovingCellShapes() {
            var frame = new CellBuffer(75, 8);
            frame.Write(0, 0, "M█", Style.Plain);
            var options = new ExportOptions { WindowChrome = true, Theme = ExportTheme.Dark, Title = "Terminal export" };
            string svg = SvgFrameWriter.Write(ConsoleExporter.Layers(frame, options), options, raster: true, ascent: 14.28);
            Assert.Contains("x=\"12\" y=\"18\" width=\"646\" height=\"179\" rx=\"12\" fill=\"#0000002e\"", svg);
            Assert.Contains("x=\"10\" y=\"14\" width=\"650\" height=\"183\" rx=\"11\" fill=\"#0000001f\"", svg);
            XElement root = XElement.Parse(svg);
            XElement text = root.Descendants().Single(e => (string?)e.Attribute("class") == "cm-t");
            Assert.Equal(14.28, N(text, "y"));
            XElement title = root.Descendants().Single(e => (string?)e.Attribute("class") == "cm-title");
            Assert.Equal(28.76, N(title, "y"));
            Assert.Contains("M8.4 0h8.4v16.8h-8.4z", svg);
        }
        private static double N(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    }
}
