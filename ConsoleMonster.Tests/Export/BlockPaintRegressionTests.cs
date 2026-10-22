using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    /// <summary>Frame height and effective style opacity stay consistent between text and vector block paints.</summary>
    public sealed class BlockPaintRegressionTests {
        [Theory]
        [InlineData(1, "█A")]
        [InlineData(2, "█A")]
        [InlineData(1, "AB")]
        [InlineData(2, "AB")]
        public void HtmlProtectsLeadingBlankRowsWithoutAddingCopiedCharacters(int blankRows, string text) {
            CellBuffer frame = new CellBuffer(2, blankRows + 1);
            frame.Write(0, blankRows, text, Style.Plain);
            XElement screen = Screen(ConsoleExporter.ToHtml(frame));
            // HTML discards an LF token directly after pre. A comment consumes that parser state without text.
            Assert.IsType<XComment>(screen.FirstNode);
            Assert.Equal(new string('\n', blankRows) + text, screen.Value);
        }

        [Theory]
        [InlineData(2, 14, 1.2)]
        [InlineData(3, 17, 1.5)]
        public void HtmlReservesTrailingBlankRowsAtTheConfiguredCellHeight(int rows, double fontSize, double lineHeight) {
            CellBuffer frame = new CellBuffer(2, rows);
            frame.Write(0, 0, "█A", Style.Plain);
            ExportOptions options = new ExportOptions { Theme = ExportTheme.Light, FontSize = fontSize, LineHeight = lineHeight };
            string html = ConsoleExporter.ToHtml(frame, options);
            XElement screen = Screen(html);
            string heightClass = Assert.Single(screen.Attribute("class")!.Value.Split(' '), c => c.StartsWith("cm-h", StringComparison.Ordinal));
            Match screenHeight = Regex.Match(html, @"\.cm-screen\." + heightClass + @"\{height:([0-9.]+)px\}");
            Match paintHeight = Regex.Match(html, @"\.cm-screen\." + heightClass + @">\.cm-paint\{height:([0-9.]+)px\}");
            Assert.True(screenHeight.Success, "The pre reserves every frame row, including the final empty line.");
            Assert.True(paintHeight.Success, "The paint viewport has the same explicit height as the frame grid.");
            double expected = rows * options.CellHeight;
            Assert.Equal(expected, double.Parse(screenHeight.Groups[1].Value, CultureInfo.InvariantCulture), 2);
            Assert.Equal(expected, double.Parse(paintHeight.Groups[1].Value, CultureInfo.InvariantCulture), 2);
            Assert.Equal("█A" + new string('\n', rows - 1), screen.Value);
            XElement vector = Assert.Single(screen.Elements(), e => e.Name.LocalName == "svg");
            Assert.Equal("0 0 16 " + rows * 16, vector.Attribute("viewBox")!.Value);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public void AutoBlockPaintComposesDimAndShadeForItsOwnStyle(int styleCase, bool svg) {
            Style style = Style.Plain;
            if (styleCase != 4) { style = style.With(TextDecoration.Dim); }
            if (styleCase == 1 || styleCase == 3) {
                style = style.WithForeground(Color.FromRgb(200, 200, 200)).WithBackground(Color.FromRgb(20, 20, 20));
            }
            if (styleCase == 2 || styleCase == 3) { style = style.With(TextDecoration.Reverse); }
            CellBuffer frame = new CellBuffer(5, 1);
            frame.Write(0, 0, "█░▒▓A", style);
            string output = svg ? ConsoleExporter.ToSvg(frame) : ConsoleExporter.ToHtml(frame);
            XElement vector = svg ? XElement.Parse(output) : Assert.Single(Screen(output).Elements(), e => e.Name.LocalName == "svg");
            double alpha = styleCase == 0 || styleCase == 2 ? 0.65 : 1;
            string fill = styleCase == 1 ? "#808080" : styleCase == 3 ? "#5c5c5c" : styleCase == 2 ? "var(--cm-bg)" : "var(--cm-fg)";
            XElement[] paths = vector.Descendants().Where(e => e.Name.LocalName == "path" && Paint(e) == fill).ToArray();
            Assert.Equal(4, paths.Length);
            double[] shades = { 1, 0.25, 0.5, 0.75 };
            for (int i = 0; i < shades.Length; i++) {
                Assert.Equal(alpha * shades[i], Opacity(paths[i]), 4);
            }
            foreach (XElement path in paths) {
                Assert.Equal(fill, Paint(path));
            }
            // A style applies only to its shapes; it must not fade the entire screen or double-dim concrete colours.
            Assert.Null(vector.Attribute("opacity"));
            Assert.Null(vector.Attribute("fill-opacity"));
        }

        [Fact]
        public void SvgDimUsesTheSameOpacityForFilledAndStrokedGlyphs() {
            CellBuffer frame = new CellBuffer(3, 1);
            frame.Write(0, 0, "╭─⣿", Style.Plain.With(TextDecoration.Dim));
            XElement svg = XElement.Parse(ConsoleExporter.ToSvg(frame));
            XElement[] paths = svg.Descendants().Where(e => e.Name.LocalName == "path").ToArray();
            XElement stroke = Assert.Single(paths, p => p.Attribute("stroke-width") != null);
            Assert.Equal("0.65", stroke.Attribute("stroke-opacity")!.Value);
            Assert.All(paths.Where(p => p.Attribute("class")?.Value == "cm-s"), p => Assert.Equal(0.65, Opacity(p)));
            Assert.Null(svg.Attribute("opacity"));
        }

        [Fact]
        public void RecordingRowsKeepNumericalDimAndIntrinsicShadeOpacity() {
            RecordingExportOptions options = new RecordingExportOptions { Theme = ExportTheme.Auto, Width = 3, Height = 2 };
            Assert.Same(ExportPalette.Dark, options.Palette);
            CellBuffer frame = new CellBuffer(3, 2);
            frame.Write(0, 0, "█▒A", Style.Plain.With(TextDecoration.Dim));
            HtmlStyles styles = new HtmlStyles();
            StringBuilder row = new StringBuilder();
            HtmlFrameWriter.AppendRow(row, frame, 0, options.Palette, styles, "s");
            XElement run = XElement.Parse(row.ToString());
            XElement[] paths = run.Descendants().Where(e => e.Name.LocalName == "path").ToArray();
            Assert.Equal(2, paths.Length);
            Assert.Equal(1, Opacity(paths[0]));
            Assert.Equal(0.5, Opacity(paths[1]));
            Assert.All(paths, p => Assert.Equal("currentColor", p.Attribute("fill")!.Value));
            StringBuilder css = new StringBuilder();
            styles.AppendCss(css);
            Assert.Contains("color:#7f7f7f;", css.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("opacity:.65", css.ToString(), StringComparison.Ordinal);
            Assert.Equal("█▒A", run.Value);
        }

        private static double Opacity(XElement path) => double.Parse(path.Attribute("fill-opacity")?.Value ?? "1", CultureInfo.InvariantCulture);

        private static string? Paint(XElement path) => path.Attribute("fill")?.Value ?? path.Attribute("style")?.Value.Replace("fill:", string.Empty);

        private static XElement Screen(string html) => XElement.Parse(Regex.Match(html, @"<pre\b[^>]*>.*?</pre>", RegexOptions.Singleline).Value, LoadOptions.PreserveWhitespace);
    }
}
