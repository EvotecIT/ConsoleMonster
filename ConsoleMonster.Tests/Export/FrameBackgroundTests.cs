using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    public sealed class FrameBackgroundTests {
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
            XElement body = Assert.Single(svg.Root!.Elements(ns + "g")).Element(ns + "rect")!;
            Assert.Equal(css, (string?)body.Attribute("fill"));
            Assert.Equal("-12", (string?)body.Attribute("x"));
            Assert.Equal("-12", (string?)body.Attribute("y"));
            Assert.Equal((string?)svg.Root.Attribute("width"), (string?)body.Attribute("width"));
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
            XElement body = Assert.Single(svg.Root!.Elements(ns + "g")).Element(ns + "rect")!;
            Assert.Equal("#ffffff", (string?)body.Attribute("fill"));
        }
    }
}
