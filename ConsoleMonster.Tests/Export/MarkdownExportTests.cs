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
        public void MarkdownUsesPipeTablesAndPreservesPresentedCulture(GlyphTier glyphs) {
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
            Assert.Contains("| Host | Status | Amount |\n| :--- | :--- | ---: |", markdown);
            Assert.Contains("| 日本 | Ready | 12,5 |", markdown);
            Assert.Equal("`````text\n" + text + "`````\n", ConsoleExporter.Export(widget, ExportFormat.FencedText, options));
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
        public void TableMarkdownEscapesCellsKeepsCollectionsAndDoesNotCropToConsoleWidth() {
            Table table = new Table { RecordViewRows = 0 };
            table.AddColumn(new TableColumn("Left|header") { Alignment = HorizontalAlignment.Left });
            table.AddColumn(new TableColumn("Center") { Alignment = HorizontalAlignment.Center });
            table.AddColumn(new TableColumn("Right") { Alignment = HorizontalAlignment.Right });
            table.AddValues("a|b\r\n<script> & *literal*", new[] { "one", "two" }, 12);
            string markdown = ConsoleExporter.ToMarkdown(table, new ExportOptions { Width = 5, Culture = CultureInfo.InvariantCulture });
            Assert.Equal("| Left\\|header | Center | Right |\n| :--- | :---: | ---: |\n| a\\|b<br>&lt;script&gt; &amp; \\*literal\\* | one, two | 12 |\n\n", markdown);
        }

        [Fact]
        public void GroupedMarkdownUsesHeadingsAndBoldSubtotalsAndFinalTotal() {
            Table table = new Table { Title = "Report", ShowTotals = true, ShowSubtotals = true };
            table.AddColumn(new TableColumn("Name") { IsKey = true });
            table.AddColumn(new TableColumn("Count") { Aggregate = ColumnAggregate.Sum });
            table.AddGroup("East").AddValues("first", 2).AddValues("second", 3);
            table.AddGroup("West").AddValues("third", 4);
            string markdown = ConsoleExporter.ToMarkdown(table, new ExportOptions { Culture = CultureInfo.InvariantCulture });
            Assert.Contains("### East · 2\n\n| Name | Count |", markdown);
            Assert.Contains("| **Subtotal** | **5** |", markdown);
            Assert.EndsWith("| **Total** | **9** |\n\n", markdown);
        }

        [Fact]
        public void RecordsAndTreesUseSemanticStructureAndExistingStatusPresenters() {
            RecordView record = new RecordView("Server");
            record.AddField("State", TableCell.FromValue("Pass"), CellPresenters.StatusWord);
            record.AddField("Notes", TableCell.FromValue("first\nsecond"));
            Tree tree = new Tree("Root");
            tree.Add("Child", StatusKind.Warn).Add("Leaf");
            string markdown = ConsoleExporter.ToMarkdown(new Rows(record, tree));
            Assert.Contains("| State | ✓ Pass |", markdown);
            Assert.Contains("| Notes | first<br>second |", markdown);
            Assert.Contains("- Root\n  - ▴ Child\n    - Leaf\n", markdown);
        }

        [Fact]
        public void CultureScopesAndCustomPresentersAreKeptInMarkdown() {
            Table table = new Table();
            table.AddColumn(new TableColumn("Size") { Presenter = CellPresenters.Bytes });
            table.AddValues(1536);
            string markdown = ConsoleExporter.ToMarkdown(new CultureScope(table, CultureInfo.GetCultureInfo("pl-PL")), new ExportOptions { Culture = CultureInfo.GetCultureInfo("en-US") });
            Assert.Contains("| 1,5 KB |", markdown);
        }

        [Fact]
        public void ContentBearingContainersKeepTitlesCaptionsStatusAndFooters() {
            Panel panel = new Panel(new TextBlock("body"), "Deployment result") { Caption = "3 hosts", Footer = "2 failures" };
            Dashboard dashboard = new Dashboard("Run result", panel) { Subtitle = "Production", Status = "Failed", StatusLevel = StatusKind.Fail, Footer = "Review failures" };
            string markdown = ConsoleExporter.ToMarkdown(dashboard, new ExportOptions { Width = 100 });
            foreach (string value in new[] { "Run result", "Production", "Failed", "Deployment result", "3 hosts", "2 failures", "body", "Review failures" }) {
                Assert.Contains(value, markdown);
            }
        }

        [Fact]
        public void UnsupportedWidgetFallbackKeepsTheEffectiveScopedCulture() {
            IWidget widget = new CultureScope(new KeyValueList().AddValue("Amount", 12.5), CultureInfo.GetCultureInfo("pl-PL"));
            string markdown = ConsoleExporter.ToMarkdown(widget, new ExportOptions { Culture = CultureInfo.GetCultureInfo("en-US") });
            Assert.Contains("12,5", markdown);
            Assert.DoesNotContain("12.5", markdown);
        }

        [Fact]
        public void SemanticMarkdownSanitizesTerminalSequencesWithoutLosingLineBreaks() {
            string input = "\u001b[31mred\u001b[0m\r\n\u001b]8;;https://example.com\u0007link\u001b]8;;\u001b\\\u0001";
            Table table = new Table();
            table.AddColumn("Value");
            table.AddValues(input);
            string markdown = ConsoleExporter.ToMarkdown(new Rows(table, new TextBlock(input) { IsMarkup = false }));
            Assert.Contains("| red<br>link |", markdown);
            Assert.EndsWith("red<br>link\n\n", markdown);
            Assert.Equal(-1, markdown.IndexOf('\u001b'));
            Assert.Equal(-1, markdown.IndexOf('\u0001'));
            Assert.DoesNotContain("https://example.com", markdown);
        }

        [Fact]
        public void LiteralProseAndTreeLabelsCannotBecomeMarkdownBlockSyntax() {
            Tree tree = new Tree("# Service");
            tree.Add("1. ordered").Add("---");
            string markdown = ConsoleExporter.ToMarkdown(new Rows(new TextBlock("---") { IsMarkup = false }, tree));
            Assert.StartsWith("\\---\n\n", markdown);
            Assert.Contains("- \\# Service\n  - 1\\. ordered\n    - \\---\n", markdown);
            Assert.Equal("export-marker\n\n", ConsoleExporter.ToMarkdown(new TextBlock("export-marker")));
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
