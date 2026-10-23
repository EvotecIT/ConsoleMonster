using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Errors {
    /// <summary>Compact error provenance and source windows remain useful within the terminal's cell budget.</summary>
    public sealed class CompactErrorContextTests {
        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void CommandHeader_UsesFailureToneAndHangsTheMessageUnderItsPrefix(int width) {
            ExceptionInfo info = new ExceptionInfo("System.InvalidOperationException", "The synthetic operation failed because the configured input file could not be read by the worker.") {
                CommandName = "Test-SyntheticErrorShape", Category = "NotSpecified"
            };
            RenderContext context = new RenderContext(TestTerminal.Profile(colors: ColorDepth.TrueColor), Theme.Dark);
            var lines = new ExceptionView(info) { Compact = true, ShowStack = false }.Lines(width, context);
            Assert.Equal("Test-SyntheticErrorShape: ", lines[0][0].Text);
            Assert.Equal(context.Style(ThemeToken.Fail).With(TextDecoration.Bold), lines[0][0].Style);
            Assert.Equal(context.Style(ThemeToken.Text), lines[0][1].Style);
            string prefix = new string(' ', lines[0][0].Text.Length);
            Assert.StartsWith(prefix, lines[1].PlainText);
            Assert.All(lines, line => Assert.True(DisplayWidth.Measure(line.PlainText) <= width, line.PlainText));
        }

        [Theory]
        [InlineData(null, "System.InvalidOperationException · inventory.json")]
        [InlineData("NotSpecified", "System.InvalidOperationException · inventory.json")]
        [InlineData("ReadError", "ReadError · inventory.json")]
        public void CompactFacts_UsesMeaningfulCategoryOrTypePlusTarget(string? category, string expected) {
            ExceptionInfo info = new ExceptionInfo("System.InvalidOperationException", "Synthetic failure.") {
                Category = category, Target = "inventory.json", ErrorId = "SyntheticFailure"
            };
            string text = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 120);
            Assert.StartsWith("Error: Synthetic failure.", text);
            Assert.Equal(expected, text.Split('\n').Last());
            Assert.DoesNotContain("NotSpecified", text);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void SourceWindow_DedentsAndElidesAroundTheFailingExtentWithAlignedCarets(int width) {
            string before = "    " + string.Concat(Enumerable.Repeat("Write-Output 'prefix'; ", 10));
            const string expression = "& $command @arguments";
            string source = before + expression + string.Concat(Enumerable.Repeat("; Write-Output 'suffix'", 10));
            ExceptionInfo info = new ExceptionInfo("Exception", "Synthetic failure.") {
                SourceLine = source, ColumnNumber = before.Length + 1, ErrorLength = expression.Length,
                ScriptPath = "synthetic.ps1", LineNumber = 4
            };
            string[] lines = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, width).Split('\n');
            int marker = Array.FindIndex(lines, line => line.Contains("^^"));
            Assert.True(marker > 0);
            string code = lines[marker - 1];
            Assert.StartsWith("…", code);
            Assert.EndsWith("…", code);
            Assert.Contains(expression, code);
            Assert.Equal(code.IndexOf(expression, StringComparison.Ordinal), lines[marker].IndexOf('^'));
            Assert.Equal(expression.Length, lines[marker].Count(c => c == '^'));
            Assert.All(lines, line => Assert.True(DisplayWidth.Measure(line) <= width, line));
        }

        [Theory]
        [InlineData("    Get-Inventory", 5, "Get-Inventory", 0)]
        [InlineData("\t'界' Get-Inventory", 6, "'界' Get-Inventory", 5)]
        [InlineData("'😀' Get-Inventory", 6, "'😀' Get-Inventory", 5)]
        public void SourceWindow_DedentsWithoutChangingOriginalCoordinatesOrGraphemeAlignment(string source, int column, string expected, int caret) {
            ExceptionInfo info = new ExceptionInfo("Exception", "Synthetic failure.") {
                SourceLine = source, ColumnNumber = column, ErrorLength = 13, ScriptPath = "synthetic.ps1", LineNumber = 9
            };
            string[] lines = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 120).Split('\n');
            int index = Array.IndexOf(lines, expected);
            Assert.True(index >= 0);
            Assert.Equal(caret, lines[index + 1].IndexOf('^'));
            Assert.Contains("synthetic.ps1:9:" + column, lines);
        }
    }
}
