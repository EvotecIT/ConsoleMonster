using System.Xml.Linq;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    public sealed class FrameBackgroundTests {
        [Theory]
        [InlineData(ExportTheme.Dark, false)]
        [InlineData(ExportTheme.Light, false)]
        [InlineData(ExportTheme.Dark, true)]
        [InlineData(ExportTheme.Light, true)]
        public void ColouredFooter_PreservesDefaultBackgroundsElsewhere(ExportTheme theme, bool blankBody) {
            CellBuffer frame = new CellBuffer(8, 3);
            if (!blankBody) { frame.Write(0, 0, "text", Style.Plain); }
            Style footer = new Style(default, Color.FromRgb(0, 255, 255));
            for (int x = 0; x < frame.Width; x++) { frame.Set(x, 2, " ", footer); }
            ExportOptions options = new ExportOptions { Theme = theme, Padding = 12 };
            XDocument svg = XDocument.Parse(ConsoleExporter.ToSvg(frame, options));
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement body = Assert.Single(svg.Root!.Descendants(ns + "g"), e => ((string?)e.Attribute("class"))?.StartsWith("cm-layer", System.StringComparison.Ordinal) == true).Element(ns + "rect")!;
            Assert.Equal(theme == ExportTheme.Dark ? "#0c0c0c" : "#ffffff", (string?)body.Attribute("fill"));
            Assert.Contains("#00ffff", svg.ToString());
            string html = ConsoleExporter.ToHtml(frame, options);
            Assert.DoesNotContain(".cm-export .cm-screen{background:#00ffff}", html);
            Assert.Contains("#00ffff", html);
        }

        [Theory]
        [InlineData(ExportTheme.Light, 60)]
        [InlineData(ExportTheme.Light, 120)]
        [InlineData(ExportTheme.Dark, 60)]
        [InlineData(ExportTheme.Dark, 120)]
        public void NativeBackground_FillsTheExportBodyIncludingPadding(ExportTheme theme, int width) {
            Color background = theme == ExportTheme.Light ? Color.FromRgb(242, 242, 242) : Color.FromRgb(12, 12, 12);
            string css = theme == ExportTheme.Light ? "#f2f2f2" : "#0c0c0c";
            CellBuffer frame = new CellBuffer(width, 3);
            frame.Clear(new Style(default, background));
            frame.Write(0, 0, "Get-Process", new Style(Color.FromRgb(13, 148, 136), background));
            frame.Write(0, 1, "prediction", new Style(Color.FromRgb(208, 208, 208), background));
            ExportOptions options = new ExportOptions { Theme = theme, WindowChrome = true, Padding = 12 };
            XDocument svg = XDocument.Parse(ConsoleExporter.ToSvg(frame, options));
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement body = Assert.Single(svg.Root!.Descendants(ns + "g"), e => ((string?)e.Attribute("class"))?.StartsWith("cm-layer", System.StringComparison.Ordinal) == true).Element(ns + "rect")!;
            Assert.Equal(css, (string?)body.Attribute("fill"));
            Assert.Equal("-11", (string?)body.Attribute("x"));
            Assert.Equal("-11", (string?)body.Attribute("y"));
            Assert.Equal(double.Parse(svg.Root.Attribute("width")!.Value, CultureInfo.InvariantCulture) - 18, double.Parse(body.Attribute("width")!.Value, CultureInfo.InvariantCulture));
            string html = ConsoleExporter.ToHtml(frame, options);
            if (theme == ExportTheme.Light) { Assert.Contains(".cm-export .cm-screen{background:" + css + "}", html); }
            else { Assert.Contains("color:var(--cm-fg);background:var(--cm-bg)", html); }
            Assert.Equal("Get-Process\nprediction\n", ConsoleExporter.ToText(frame));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NonUniformOrOccupiedBottomRow_DoesNotExtendAnInteriorPanel(bool occupied) {
            CellBuffer frame = new CellBuffer(4, 2);
            Style panel = new Style(default, Color.FromRgb(242, 242, 242));
            frame.Clear(panel);
            frame.Set(3, 1, occupied ? "x" : " ", occupied ? panel : Style.Plain);
            XDocument svg = XDocument.Parse(ConsoleExporter.ToSvg(frame, new ExportOptions { Theme = ExportTheme.Light, WindowChrome = true }));
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement body = Assert.Single(svg.Root!.Descendants(ns + "g"), e => ((string?)e.Attribute("class"))?.StartsWith("cm-layer", System.StringComparison.Ordinal) == true).Element(ns + "rect")!;
            Assert.Equal("#ffffff", (string?)body.Attribute("fill"));
        }
    }
}
