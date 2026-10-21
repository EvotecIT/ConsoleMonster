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
        [Theory]
        [InlineData(45)]
        [InlineData(120)]
        public void CommandLine_ValueCatalog_KeepsLiteralIdsAndOwnDescriptionColumns(int width) {
            HelpTopic topic = new HelpTopic("tool report") { CommandLine = new CommandLineHelp {
                Usage = "tool report [<type>...] [options]", ValuesHeading = "Report types"
            } };
            topic.CommandLine.Values.Add(new HelpValue("group-policy", "Policy consistency and permissions with a sufficiently long description to wrap at narrow widths.", "Directory"));
            topic.CommandLine.Values.Add(new HelpValue("group-members", "Saved membership evidence [offline].", "Directory"));
            string text = Text(new HelpView(topic), width);
            Assert.Contains("Report types", text);
            Assert.Contains("group-policy", text);
            Assert.Contains("group-members", text);
            Assert.Contains("[offline]", text);
            Assert.DoesNotContain("Commands", text);
            if (width == 45) {
                string[] lines = text.Replace("\r", "").Split('\n');
                int row = Array.FindIndex(lines, line => line.Contains("group-policy"));
                Assert.StartsWith(new string(' ', 17), lines[row + 1]);
            }
        }

        [Theory]
        [InlineData(45)]
        [InlineData(120)]
        public void CommandLine_ColumnsPreserveSyntaxAndWrapDescriptionsIndependently(int width) {
            HelpTopic topic = new HelpTopic("tool") {
                Synopsis = "Inspect saved findings.",
                CommandLine = new CommandLineHelp { Usage = "tool [command] [options]" }
            };
            topic.CommandLine.Commands.Add(new HelpCommand("inspect", "Inspect findings saved by a previous discovery run.", "Discovery"));
            topic.Parameters.Add(new HelpParameter("-output") { Type = "path", Required = true, Description = "Write the resulting report to this destination.", Default = "report.html" });
            topic.Parameters[0].Aliases.Add("-o");
            topic.Parameters.Add(new HelpParameter("-an-unusually-long-option") { Type = "path", Description = "Keep the complete syntax." });
            topic.Examples.Add(new HelpExample(null, "tool inspect --output report.html"));

            string text = Text(new HelpView(topic), width);
            Assert.Contains("tool [command] [options]", text);
            Assert.Contains("Discovery", text);
            Assert.Contains("-o, --output <path>", text);
            Assert.Equal(1, text.Split(new[] { "Required." }, StringSplitOptions.None).Length - 1);
            Assert.Contains("--an-unusually-long-option <path>", text);
            Assert.Contains("[default: report.html]", System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " "));
            Assert.Contains("Examples", text);
            string[] lines = text.Replace("\r", "").Split('\n');
            int command = Array.FindIndex(lines, l => l.Contains("inspect") && l.Contains("Inspect findings"));
            Assert.True(command >= 0);
            if (width == 45) Assert.StartsWith(new string(' ', 11), lines[command + 1]);
        }

        [Theory]
        [InlineData(45)]
        [InlineData(120)]
        public void CommandLine_GroupsShareDescriptionColumnAndSeparateHeadings(int width) {
            HelpTopic topic = new HelpTopic("tool") { CommandLine = new CommandLineHelp { Usage = "tool [command] [options]" } };
            topic.CommandLine.Commands.Add(new HelpCommand("long-command", "First description", "First group"));
            topic.CommandLine.Commands.Add(new HelpCommand("short", "Second description", "Second group"));
            topic.Parameters.Add(new HelpParameter("-quiet") { Description = "Hide details", Default = "no" });
            topic.Parameters.Add(new HelpParameter("-output") { Description = "Write output", Type = "path", Default = "report.html" });
            string text = Text(new HelpView(topic), width);
            string[] lines = text.Replace("\r", "").Split('\n');
            string first = Array.Find(lines, line => line.Contains("First description"))!;
            string second = Array.Find(lines, line => line.Contains("Second description"))!;
            Assert.Equal(first.IndexOf("First description", StringComparison.Ordinal), second.IndexOf("Second description", StringComparison.Ordinal));
            int group = Array.FindIndex(lines, line => line.Contains("Second group"));
            Assert.True(string.IsNullOrWhiteSpace(lines[group - 1]));
            Assert.DoesNotContain("[default: no]", text);
            Assert.Contains("[default: report.html]", System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " "));
        }

        [Fact]
        public void Table_ExplicitTitleIndent_AlignsSectionTitleWithoutChangingDefault() {
            Table table = new Table { Title = "Per-CA", TitleIndent = 0 };
            table.AddColumn("CA");
            table.AddValues("Issuing CA");
            Assert.StartsWith("Per-CA", Text(table, 45));
            table.TitleIndent = null;
            Assert.StartsWith(" Per-CA", Text(table, 45));
            table.Title = "01234567890123456789";
            table.TitleIndent = 4;
            string firstLine = Text(table, 20).Replace("\r", "").Split('\n')[0].TrimEnd();
            Assert.StartsWith("    0123456789", firstLine);
            Assert.EndsWith("\u2026", firstLine);
        }

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
