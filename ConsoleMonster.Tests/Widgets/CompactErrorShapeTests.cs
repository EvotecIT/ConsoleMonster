using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Errors {
    /// <summary>Compact errors retain the useful message without repeated causes or embedded stack dumps.</summary>
    public sealed class CompactErrorShapeTests {
        [Fact]
        public void DuplicateInnerMessage_IsShownOnceWithItsTypeInTheSummary() {
            const string message = "Object reference not set to an instance of an object";
            ExceptionInfo info = new ExceptionInfo("CmdletInvocationException", message) {
                Inner = new ExceptionInfo("NullReferenceException", message)
            };
            string text = Render(info);
            Assert.Equal(1, text.Split(new[] { message }, StringSplitOptions.None).Length - 1);
            Assert.Contains("+1 inner exception (NullReferenceException)", text);
        }

        [Fact]
        public void GenericOuterMessage_ShowsTheInnermostDistinctCauseOnce() {
            ExceptionInfo info = new ExceptionInfo("Exception", "An error occurred while creating the pipeline.") {
                Inner = new ExceptionInfo("Exception", "An error occurred while creating the pipeline.") {
                    Inner = new ExceptionInfo("IOException", "The synthetic inventory cache is locked by another writer.")
                }
            };
            string text = Render(info);
            Assert.Contains("cause: The synthetic inventory cache is locked by another writer.", text);
            Assert.Equal(1, text.Split(new[] { "An error occurred while creating the pipeline." }, StringSplitOptions.None).Length - 1);
            Assert.Contains("+2 inner exceptions", text);
        }

        [Fact]
        public void EmbeddedStack_IsCountedWithoutLosingOtherMessageParagraphs() {
            string stack = string.Join("\n", Enumerable.Range(1, 20).Select(n => "   at System.Synthetic.Worker.Step" + n + "()"));
            ExceptionInfo info = new ExceptionInfo("InvalidOperationException", "Runspace execution failed.\n" + stack + "\nRetry the synthetic operation.");
            string text = Render(info);
            Assert.Contains("Error: Runspace execution failed.", text);
            Assert.Contains("Retry the synthetic operation.", text);
            Assert.Contains("20 stack lines hidden", text);
            Assert.DoesNotContain("System.Synthetic", text);
            Assert.True(text.Split('\n').Length <= 5, text);
            string detailed = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = true }, 120);
            Assert.Contains("at System.Synthetic.Worker.Step20()", detailed);
        }

        [Fact]
        public void MessageMentioningAt_IsNotMistakenForAStackFrame() {
            string text = Render(new ExceptionInfo("Exception", "The service failed at System startup.\n   at least one source (required) must exist."));
            Assert.Contains("at System startup.", text);
            Assert.Contains("at least one source (required) must exist.", text);
            Assert.DoesNotContain("stack lines hidden", text);
        }

        [Fact]
        public void WrappedCause_ContinuesUnderItsMessage() {
            ExceptionInfo info = new ExceptionInfo("Exception", "Pipeline creation failed.") {
                Inner = new ExceptionInfo("IOException", "The synthetic inventory cache is locked by another writer.")
            };
            string[] lines = TestTerminal.RenderText(new ExceptionView(info) { Compact = true, ShowStack = false }, 60).Split('\n');
            int index = Array.FindIndex(lines, line => line.StartsWith("cause:", StringComparison.Ordinal));
            Assert.True(index >= 0);
            Assert.StartsWith("       ", lines[index + 1]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DistinctCauseAndStackSummary_UseMutedThemeTone(bool light) {
            ExceptionInfo info = new ExceptionInfo("Exception", "Runspace failed.\n   at System.Synthetic.Step()") {
                Inner = new ExceptionInfo("IOException", "Inventory is locked.")
            };
            RenderContext context = new RenderContext(TestTerminal.Profile(colors: ColorDepth.TrueColor), light ? Theme.Light : Theme.Dark);
            var lines = new ExceptionView(info) { Compact = true, ShowStack = false }.Lines(120, context);
            Assert.All(lines.Single(line => line.PlainText.StartsWith("cause:", StringComparison.Ordinal)), span => Assert.Equal(context.Style(ThemeToken.Muted), span.Style));
            Assert.All(lines.Single(line => line.PlainText.StartsWith("1 stack line hidden", StringComparison.Ordinal)), span => Assert.Equal(context.Style(ThemeToken.Muted), span.Style));
        }

        private static string Render(ExceptionInfo info) => TestTerminal.RenderText(
            new ExceptionView(info) { Compact = true, ShowStack = false, DetailsHint = "Get-Error for details" }, 120);
    }
}
