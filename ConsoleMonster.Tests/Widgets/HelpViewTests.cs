using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Help;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.HelpViews {
    /// <summary>A command's help: syntax wrapping, the parameter table's columns, synopsis and examples.</summary>
    public sealed class HelpViewTests {
        private static string Text(IWidget widget, int width) {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(width, 200, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Size size = widget.Measure(new Constraints(width, 200), context);
            CellBuffer buffer = new CellBuffer(width, Math.Max(1, size.Height));
            widget.Render(new Region(buffer), context);
            return buffer.ToPlainText();
        }

        private static HelpTopic Sample() {
            HelpTopic topic = new HelpTopic("Copy-Thing") { Synopsis = "Copies things between stores.", CommonParameters = true };
            HelpSyntax syntax = new HelpSyntax("Copy-Thing") { CommonParameters = true };
            syntax.Parameters.Add(new HelpSyntaxParameter("Path") { Type = "string[]", Required = true, Positional = true });
            syntax.Parameters.Add(new HelpSyntaxParameter("Destination") { Type = "string", Positional = true });
            syntax.Parameters.Add(new HelpSyntaxParameter("Filter") { Type = "string" });
            syntax.Parameters.Add(new HelpSyntaxParameter("Force"));
            topic.Syntax.Add(syntax);
            topic.Parameters.Add(new HelpParameter("Path") { Type = "string[]", Required = true, Position = "0", Pipeline = "ByValue" });
            topic.Parameters.Add(new HelpParameter("Force") { Type = "switch" });
            topic.Examples.Add(new HelpExample(null, "Copy-Thing a b") { Remarks = "Copies a to b." });
            topic.Examples.Add(new HelpExample("Everything at once", "Copy-Thing * c -Force"));
            return topic;
        }

        [Fact]
        public void Syntax_CommandLineArguments_RenderWithoutInventedOptionNames() {
            HelpTopic topic = new HelpTopic("tool export");
            HelpSyntax syntax = new HelpSyntax("tool export");
            syntax.Parameters.Add(new HelpSyntaxParameter("path") { Named = false, Required = true });
            syntax.Parameters.Add(new HelpSyntaxParameter("command") { Named = false });
            syntax.Parameters.Add(new HelpSyntaxParameter("-format") { Type = "string" });
            topic.Syntax.Add(syntax);
            topic.Parameters.Add(new HelpParameter("path") { Named = false, Required = true, Type = "string", Position = "0" });

            string text = Text(new HelpView(topic), 120);

            Assert.Contains("tool export <path> [<command>] [--format <string>]", text);
            Assert.DoesNotContain("-path", text);
            Assert.DoesNotContain("-command", text);
        }

        [Fact]
        public void Syntax_MarksRequiredOptionalAndPositionalParts() {
            string text = Text(new HelpView(Sample()), 120);
            Assert.Contains("Copy-Thing [-Path] <string[]> [[-Destination] <string>] [-Filter <string>] [-Force] [<CommonParameters>]", text);
        }

        [Fact]
        public void Syntax_WrapsBetweenParametersUnderTheFirstOne() {
            string[] lines = Text(new HelpView(Sample()), 60).Replace("\r", string.Empty).Split('\n');
            int first = Array.FindIndex(lines, line => line.Contains("Copy-Thing [-Path]"));
            Assert.True(first >= 0);
            Assert.StartsWith(new string(' ', 13) + "[", lines[first + 1]);
        }

        [Fact]
        public void ParameterTable_LeavesOutColumnsNobodyFills() {
            string text = Text(new HelpView(Sample()), 120);
            Assert.Contains("Pipeline", text);
            Assert.DoesNotContain("Default", text);
            Assert.DoesNotContain("Description", text);
        }

        [Fact]
        public void Synopsis_ThatRepeatsTheSyntax_IsLeftOut() {
            HelpTopic topic = Sample();
            HelpTopic generated = new HelpTopic("Copy-Thing") { Synopsis = "Copy-Thing [-Path] <string[]> [-Force]" };
            generated.Syntax.Add(topic.Syntax[0]);
            string text = Text(new HelpView(generated), 120);
            string beforeSyntax = text.Substring(0, text.IndexOf("Syntax", StringComparison.Ordinal));
            Assert.DoesNotContain("[-Path]", beforeSyntax);
            Assert.Contains("Copies things between stores.", Text(new HelpView(topic), 120));
        }

        [Fact]
        public void Examples_AreNumberedWithTheirTitles() {
            string text = Text(new HelpView(Sample()), 120);
            Assert.Contains("Example 1", text);
            Assert.Contains("2. Everything at once", text);
            Assert.Contains("Copies a to b.", text);
            Assert.DoesNotContain("Example 1", Text(new HelpView(Sample()) { ShowExamples = false }, 120));
        }
    }
}
