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
    public sealed class CompactExceptionViewTests {
        private static ExceptionInfo Sample() {
            ExceptionInfo info = new ExceptionInfo("InvalidOperationException", "Loading the inventory failed.") {
                SourceLine = "    Get-Inventory -Name 'production'",
                ScriptPath = "deploy.ps1", LineNumber = 12, ColumnNumber = 5, ErrorLength = 13,
                Category = "InvalidOperation", Target = "production", ErrorId = "LoadFailed",
                Inner = new ExceptionInfo("IOException", "Disk unavailable.") { Inner = new ExceptionInfo("Exception", "Root cause.") }
            };
            info.ScriptFrames.Add(new ExceptionFrame("Get-Inventory", "deploy.ps1", 12, false));
            return info;
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void CompactMessageInvocationAndFoldedCausesFitTheBudget(int width) {
            string text = TestTerminal.RenderText(new ExceptionView(Sample()) { Compact = true, ShowStack = false, DetailsHint = "Get-Error for details" }, width);
            Assert.StartsWith("Error: Loading the inventory failed.", text);
            Assert.Contains("deploy.ps1:12:5", text);
            Assert.Contains("    Get-Inventory -Name 'production'\n    ^^^^^^^^^^^^^", text);
            Assert.Contains("InvalidOperation", text);
            Assert.Contains("production", text);
            Assert.Contains("+2 inner exceptions · Get-Error for details", text);
            Assert.DoesNotContain("Disk unavailable", text);
            Assert.DoesNotContain("at Get-Inventory", text);
            Assert.EndsWith("Get-Error for details", text);
            Assert.All(text.Split('\n'), line => Assert.True(DisplayWidth.Measure(line) <= width, line));
        }

        [Fact]
        public void DetailedCompactViewShowsStacksAndCauses() {
            string text = TestTerminal.RenderText(new ExceptionView(Sample()) { Compact = true, ShowStack = true, DetailsHint = "Get-Error for details" }, 100);
            Assert.Contains("at Get-Inventory", text);
            Assert.Contains("Caused by IOException: Disk unavailable.", text);
            Assert.Contains("Caused by Exception: Root cause.", text);
            Assert.DoesNotContain("+2 inner exceptions", text);
            Assert.EndsWith("Get-Error for details", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompactInvocationCaretUsesDimFailureToneWithoutTintingTheMessage(bool light) {
            RenderContext context = new RenderContext(TestTerminal.Profile(colors: ColorDepth.TrueColor), light ? Theme.Light : Theme.Dark);
            var lines = new ExceptionView(Sample()) { Compact = true, ShowStack = false }.Lines(60, context);
            StyledLine marker = lines.Single(line => line.PlainText.TrimStart().StartsWith("^", StringComparison.Ordinal));
            Assert.All(marker, span => Assert.Equal(context.Style(ThemeToken.Fail).With(TextDecoration.Dim), span.Style));
            Assert.Equal(context.Style(ThemeToken.Text), lines[0][1].Style);
        }

        [Theory]
        [InlineData(true, false, "+2 inner exceptions")]
        [InlineData(false, true, "Get-Error for details")]
        public void CompactFooterOmitsTheSeparatorWhenEitherPartIsAbsent(bool inner, bool hint, string expected) {
            ExceptionInfo info = Sample();
            if (!inner) { info.Inner = null; }
            string text = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false,
                DetailsHint = hint ? "Get-Error for details" : null }, 60);
            Assert.Equal(expected, text.Split('\n').Last());
        }

        [Fact]
        public void CompactMessageKeepsItsHangingIndentAtSixtyColumns() {
            string message = "Cannot load the inventory because the configured source file is missing and the backup source is unavailable.";
            ExceptionInfo info = new ExceptionInfo("InvalidOperationException", message) { ScriptPath = "deploy.ps1", LineNumber = 12 };
            string[] rows = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 60).Split('\n')
                .TakeWhile(row => !row.StartsWith("deploy.ps1:", StringComparison.Ordinal)).ToArray();
            Assert.True(rows.Length > 1);
            Assert.StartsWith("Error: ", rows[0]);
            Assert.All(rows.Skip(1), row => Assert.StartsWith("       ", row));
            Assert.Equal(message, string.Join(" ", rows.Select(row => row.Substring(7).TrimEnd())));
            Assert.All(rows, row => Assert.True(DisplayWidth.Measure(row) <= 60));
        }

        [Theory]
        [InlineData("\t'界' Get-Inventory", 6, 9)]
        [InlineData("'😀' Get-Inventory", 6, 5)]
        public void InvocationMarkerUsesTerminalCellsInsteadOfUtf16Offsets(string line, int column, int expectedCells) {
            ExceptionInfo info = Sample();
            info.SourceLine = line;
            info.ColumnNumber = column;
            string text = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 120);
            string marker = text.Split('\n').Single(row => row.TrimStart().StartsWith("^", StringComparison.Ordinal));
            Assert.Equal(expectedCells, marker.IndexOf('^'));
        }

        [Fact]
        public void CompactLabelUsesFailureToneAndProfileLanguage() {
            TerminalProfile profile = TestTerminal.Profile(colors: ColorDepth.TrueColor).WithLanguage(System.Globalization.CultureInfo.GetCultureInfo("pl"));
            RenderContext context = new RenderContext(profile, Theme.Light);
            var lines = new ExceptionView(Sample()) { Compact = true, ShowStack = false }.Lines(80, context);
            Assert.Equal("Błąd: ", lines[0][0].Text);
            Assert.Equal(context.Style(ThemeToken.Fail).With(TextDecoration.Bold), lines[0][0].Style);
            Assert.Equal(context.Style(ThemeToken.Text), lines[0][1].Style);
            Assert.Contains(lines, line => line.PlainText == "+2 wyjątków wewnętrznych");
        }

        [Fact]
        public void NarrowSourceKeepsFailingExpressionAndMarkerVisible() {
            ExceptionInfo info = Sample();
            info.SourceLine = new string(' ', 100) + "Get-Inventory -Name 'production'";
            info.ColumnNumber = 101;
            string text = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 30);
            Assert.Contains("Get-Inventory", text);
            Assert.Contains("^^^^^^^^^^^^^", text);
            Assert.All(text.Split('\n'), line => Assert.True(DisplayWidth.Measure(line) <= 30, line));
        }
    }
}
