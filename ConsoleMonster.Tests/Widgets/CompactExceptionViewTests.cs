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
            Assert.Contains("+2 inner exceptions", text);
            Assert.DoesNotContain("Disk unavailable", text);
            Assert.DoesNotContain("at Get-Inventory", text);
            Assert.EndsWith("Get-Error for details", text);
            Assert.All(text.Split('\n'), line => Assert.True(DisplayWidth.Measure(line) <= width, line));
        }

        [Fact]
        public void DetailedCompactViewShowsStacksAndCauses() {
            string text = TestTerminal.RenderText(new ExceptionView(Sample()) { Compact = true, ShowStack = true }, 100);
            Assert.Contains("at Get-Inventory", text);
            Assert.Contains("Caused by IOException: Disk unavailable.", text);
            Assert.Contains("Caused by Exception: Root cause.", text);
            Assert.DoesNotContain("+2 inner exceptions", text);
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
        public void CompactLabelAloneUsesFailureToneAndProfileLanguage() {
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
