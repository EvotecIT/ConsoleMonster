using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    public sealed class MarkdownExportTests {
        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void MarkdownPreservesTheSameRenderedTableAndCultureAsText(GlyphTier glyphs) {
            Table table = new Table();
            table.AddColumn("Host");
            table.AddColumn("Status");
            table.AddColumn("Amount");
            table.Columns[2].Presenter = new NumberPresenter();
            table.AddRow("日本", "[pass]Ready[/]", TableCell.FromValue(12.5));
            IWidget widget = new Rows(table, new TextBlock("**literal** <script>\n```\n````") { IsMarkup = false });
            ExportOptions options = new ExportOptions { Width = 35, Glyphs = glyphs, Culture = CultureInfo.GetCultureInfo("pl-PL") };
            string text = ConsoleExporter.ToText(widget, options);
            string markdown = ConsoleExporter.Export(widget, ExportFormat.Markdown, options);
            Assert.Equal("`````text\n" + text + "`````\n", markdown);
            Assert.Contains("Ready", markdown);
            Assert.Contains("12,5", markdown);
            Assert.DoesNotContain("[pass]", markdown);
            Assert.Equal(-1, markdown.IndexOf('\u001b'));
        }

        [Fact]
        public void FrameMarkdownKeepsWideGlyphsLiteralTextAndSize() {
            CellBuffer frame = new CellBuffer(30, 3);
            frame.Write(0, 0, "日本 **bold** | <tag>", Style.Plain);
            frame.Write(0, 1, "  ``` trailing", Style.Plain);
            Assert.Equal("````text\n" + ConsoleExporter.ToText(frame) + "````\n", ConsoleExporter.Export(frame, ExportFormat.Markdown));
        }

        [Fact]
        public void EmptyFrameIsAClosedCodeBlock() =>
            Assert.Equal("```text\n```\n", ConsoleExporter.ToMarkdown(new CellBuffer(0, 0)));

        [Theory]
        [InlineData("report.md")]
        [InlineData("report.MARKDOWN")]
        public void FileExtensionsSelectMarkdown(string path) => Assert.Equal(ExportFormat.Markdown, ConsoleExporter.FormatFor(path));

        [Fact]
        public void NullWidgetAndFrameAreRejected() {
            Assert.Throws<ArgumentNullException>(() => ConsoleExporter.ToMarkdown((IWidget)null!));
            Assert.Throws<ArgumentNullException>(() => ConsoleExporter.ToMarkdown((CellBuffer)null!));
        }
    }
}
