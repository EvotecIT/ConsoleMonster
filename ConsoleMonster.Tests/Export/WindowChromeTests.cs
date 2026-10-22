using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    /// <summary>Window chrome keeps captions in their own box at narrow export widths.</summary>
    public sealed class WindowChromeTests {
        public static IEnumerable<object[]> Frames() {
            foreach (int columns in new[] { 20, 36, 60 }) {
                foreach (bool longTitle in new[] { false, true }) {
                    foreach (ExportTheme theme in new[] { ExportTheme.Light, ExportTheme.Dark }) {
                        yield return new object[] { columns, longTitle, theme };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Frames))]
        public void SvgTitleBoxClearsDotsAndRightEdge(int columns, bool longTitle, ExportTheme theme) {
            string title = Caption(longTitle);
            string markup = ConsoleExporter.ToSvg(Frame(columns), Options(title, theme));
            XElement svg = XElement.Parse(markup);
            XElement area = Assert.Single(svg.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
            XElement text = Assert.Single(area.Elements(), e => e.Name.LocalName == "text");
            double dotsRight = svg.Elements().Where(e => e.Name.LocalName == "circle").Max(e => Number(e, "cx") + Number(e, "r"));
            double left = Number(area, "x");
            double available = Number(area, "width");
            double centre = left + Number(text, "x");
            double textWidth = Number(text, "textLength");
            Assert.True(left >= dotsRight + 12);
            Assert.Equal(Number(svg, "width") - 12, left + available, 2);
            Assert.Equal(left + available / 2, centre, 2);
            Assert.True(centre - textWidth / 2 >= left);
            Assert.True(centre + textWidth / 2 <= left + available);
            Assert.Equal("hidden", (string?)area.Attribute("overflow"));
            Assert.Equal("spacingAndGlyphs", (string?)text.Attribute("lengthAdjust"));
            Assert.Equal(title, Assert.Single(svg.Elements(), e => e.Name.LocalName == "title").Value);
            Assert.Equal(title, Assert.Single(text.Elements()).Value);
            string visible = string.Concat(text.Nodes().OfType<XText>().Select(n => n.Value));
            if (longTitle) { Assert.EndsWith("…", visible); }
            else { Assert.Equal(title, visible); }
            if (columns == 20 && longTitle) { Golden.Match("export/chrome." + theme.ToString().ToLowerInvariant() + ".svg", markup); }
        }

        [Theory]
        [MemberData(nameof(Frames))]
        public void HtmlTitleBoxClearsDotsAndRetainsTooltip(int columns, bool longTitle, ExportTheme theme) {
            string title = Caption(longTitle);
            string html = ConsoleExporter.ToHtml(Frame(columns), Options(title, theme));
            string fragment = html.Substring(html.IndexOf("<div class=\"cm-export\">", StringComparison.Ordinal));
            fragment = fragment.Substring(0, fragment.IndexOf("</body>", StringComparison.Ordinal)).Trim();
            XElement root = XElement.Parse(fragment);
            XElement caption = Assert.Single(root.Descendants(), e => (string?)e.Attribute("class") == "cm-title");
            Assert.Equal(title, caption.Value);
            Assert.Equal(title, (string?)caption.Attribute("title"));
            // These CSS boxes remain disjoint regardless of the browser's installed monospace font.
            string dots = Rule(html, ".cm-export .cm-dots");
            string dot = Rule(html, ".cm-export .cm-dots i");
            string layout = Rule(html, ".cm-export .cm-titlebar .cm-title");
            double dotsRight = Pixels(dots, "left") + 3 * Pixels(dot, "width") + 2 * 8;
            Assert.True(Pixels(layout, "left") >= dotsRight + 12);
            Assert.Equal(12, Pixels(layout, "right"));
            Assert.Contains("text-align:center", layout);
            Assert.Contains("text-overflow:ellipsis", layout);
            Assert.Contains("overflow:hidden", layout);
            Assert.Contains("white-space:nowrap", layout);
            Assert.Contains("@container (width < 96px)", html);
            Assert.Contains(".cm-title{visibility:hidden}", html);
            if (columns == 20 && longTitle) { Golden.Match("export/chrome." + theme.ToString().ToLowerInvariant() + ".html", html); }
        }

        [Theory]
        [InlineData(1, 4, 0)]
        [InlineData(8, 14, 0)]
        [InlineData(10, 14, 4)]
        public void TinyFramesKeepAllDotsAndHideCaptionWhenEllipsisCannotFit(int columns, double fontSize, int padding) {
            ExportOptions options = Options("A long title", ExportTheme.Dark);
            options.FontSize = fontSize;
            options.Padding = padding;
            XElement svg = XElement.Parse(ConsoleExporter.ToSvg(Frame(columns), options));
            double right = svg.Elements().Where(e => e.Name.LocalName == "circle").Max(e => Number(e, "cx") + Number(e, "r"));
            Assert.True(Number(svg, "width") >= right + 12);
            Assert.DoesNotContain(svg.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
            string html = ConsoleExporter.ToHtml(Frame(columns), options);
            Assert.Contains(".cm-export .cm-window{min-width:76px}", html);
            Assert.Contains("@container (width < 96px)", html);
        }

        [Fact]
        public void SvgShowsOnlyEllipsisWhenThatIsAllTheTitleAreaCanFit() {
            ExportOptions options = Options("Long title", ExportTheme.Light);
            options.FontSize = 16;
            options.Padding = 0;
            XElement svg = XElement.Parse(ConsoleExporter.ToSvg(Frame(10), options));
            XElement area = Assert.Single(svg.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
            XElement text = Assert.Single(area.Elements());
            Assert.Equal(8, Number(area, "width"));
            Assert.Equal("…", string.Concat(text.Nodes().OfType<XText>().Select(n => n.Value)));
            Assert.Equal(8, Number(text, "textLength"));
        }

        [Fact]
        public void SvgTruncationKeepsJoinedGraphemesAndLiteralTooltipText() {
            string title = "👩‍💻 " + new string('界', 30) + " <__TEXT__> & \"__FULL__\"";
            XElement svg = XElement.Parse(ConsoleExporter.ToSvg(Frame(20), Options(title, ExportTheme.Dark)));
            XElement area = Assert.Single(svg.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
            XElement text = Assert.Single(area.Elements());
            Assert.Equal(title, Assert.Single(text.Elements()).Value);
            string visible = string.Concat(text.Nodes().OfType<XText>().Select(n => n.Value));
            Assert.Equal("👩‍💻 界界界界…", visible);
        }

        private static string Caption(bool longTitle) => longTitle ? "Long labels / 20 columns / " + new string('W', 90) : "Chart";
        private static ExportOptions Options(string title, ExportTheme theme) => new ExportOptions { WindowChrome = true, Title = title, Theme = theme };
        private static CellBuffer Frame(int columns) {
            CellBuffer frame = new CellBuffer(columns, 1);
            frame.Write(0, 0, "x", Style.Plain);
            return frame;
        }
        private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        private static string Rule(string html, string selector) => Regex.Match(html, Regex.Escape(selector) + @"\{([^}]+)\}").Groups[1].Value;
        private static double Pixels(string rule, string property) => double.Parse(Regex.Match(rule, @"(?:^|;)" + property + @":([0-9.]+)px").Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
