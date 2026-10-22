using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    /// <summary>Continuous block paints keep bar and histogram interiors solid at fractional browser cell widths.</summary>
    public sealed class BlockRunExportTests {
        [Theory]
        [InlineData(0x2588)]
        [InlineData(0x2586)]
        [InlineData(0x2592)]
        public void HtmlRepeatedBlocksShareOnePaintAndKeepCopiedCells(int codePoint) {
            string glyphs = new string((char)codePoint, 37);
            CellBuffer frame = new CellBuffer(40, 1);
            frame.Write(0, 0, glyphs, Style.Plain);
            string html = ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Light });
            XElement screen = Screen(html);
            XElement paint = Assert.Single(screen.Elements(), e => e.Name.LocalName == "span");
            Assert.Equal(glyphs, paint.Value);
            Assert.Equal(glyphs, screen.Value);
            Assert.Contains("bc37", paint.Attribute("class")!.Value.Split(' '));
            Assert.Equal("37", Regex.Match(html, @"\.bc37\{width:(\d+)ch\}").Groups[1].Value);
            XElement vector = Assert.Single(screen.Elements(), e => e.Name.LocalName == "svg");
            Assert.Equal("0 0 320 16", vector.Attribute("viewBox")!.Value);
            Assert.Equal("true", vector.Attribute("aria-hidden")!.Value);
            XElement shape = Assert.Single(vector.Elements());
            Assert.Equal(1, shape.Attribute("d")!.Value.Count(c => c == 'M'));
            Assert.Equal("#24292f", shape.Attribute("fill")!.Value);
            Assert.Contains("shape-rendering:crispEdges", html, StringComparison.Ordinal);
        }

        [Fact]
        public void HtmlBlockRunsStopAtStyleSpacesAndNonrepeatingShapes() {
            CellBuffer frame = new CellBuffer(12, 1);
            frame.Write(0, 0, "██", Style.Plain);
            frame.Write(2, 0, "██", Style.Plain.WithForeground(Color.FromRgb(255, 0, 0)));
            frame.Write(5, 0, "██▐▐", Style.Plain);
            XElement screen = Screen(ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Light }));
            XElement[] blocks = screen.Descendants().Where(e => (e.Attribute("class")?.Value.Split(' ') ?? Array.Empty<string>()).Contains("b")).ToArray();
            Assert.Equal(new[] { "██", "██", "██", "▐", "▐" }, blocks.Select(e => e.Value));
            Assert.Equal("████ ██▐▐", screen.Value);
            Assert.NotEqual(blocks[0].Parent, blocks[1].Parent);
        }

        [Fact]
        public void HtmlTouchingRowsShareOneVectorViewportAndKeepEveryCopiedLine() {
            string row = new string('█', 37);
            CellBuffer frame = new CellBuffer(37, 2);
            frame.Write(0, 0, row, Style.Plain);
            frame.Write(0, 1, row, Style.Plain);
            XElement screen = Screen(ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Light }));
            XElement vector = Assert.Single(screen.Elements(), e => e.Name.LocalName == "svg");
            Assert.Equal("0 0 296 32", vector.Attribute("viewBox")!.Value);
            XElement path = Assert.Single(vector.Elements());
            Assert.Equal("M0 0h296v16h-296zM0 16h296v16h-296z", path.Attribute("d")!.Value);
            Assert.Equal(row + "\n" + row, screen.Value);
        }

        [Fact]
        public void SvgRepeatedBlocksUseOneContinuousRectanglePerRow() {
            CellBuffer frame = new CellBuffer(37, 2);
            frame.Write(0, 0, new string('█', 37), Style.Plain);
            frame.Write(0, 1, new string('█', 37), Style.Plain);
            ExportOptions options = new ExportOptions { Theme = ExportTheme.Light };
            XElement svg = XElement.Parse(ConsoleExporter.ToSvg(frame, options));
            XNamespace ns = svg.Name.Namespace;
            string[] paths = svg.Descendants(ns + "path").Select(e => e.Attribute("d")!.Value).ToArray();
            Assert.Equal(2, paths.Length);
            foreach (string path in paths) {
                Assert.Equal(1, path.Count(c => c == 'M'));
                double width = double.Parse(Regex.Match(path, @"h([0-9.]+)").Groups[1].Value, CultureInfo.InvariantCulture);
                Assert.Equal(37 * options.CellWidth, width, 2);
            }
        }

        private static XElement Screen(string html) => XElement.Parse(Regex.Match(html, @"<pre\b[^>]*>.*?</pre>", RegexOptions.Singleline).Value, LoadOptions.PreserveWhitespace);
    }
}
