using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Help;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CliExampleLineTests {
        [Theory]
        [InlineData("lf", 60)]
        [InlineData("lf", 120)]
        [InlineData("crlf", 60)]
        [InlineData("crlf", 120)]
        [InlineData("cr", 60)]
        [InlineData("cr", 120)]
        public void CommandLineExamples_PreserveCommandBoundariesBlankLinesAndIndentation(string ending, int width) {
            string newline = ending == "crlf" ? "\r\n" : ending == "cr" ? "\r" : "\n";
            string[] lines = Render("atlas inspect" + newline + newline + "    atlas export --output \"[raw].html\"", width);
            int first = Array.IndexOf(lines, "  atlas inspect");
            Assert.True(first >= 0);
            Assert.Equal(string.Empty, lines[first + 1]);
            Assert.Equal("      atlas export --output \"[raw].html\"", lines[first + 2]);
        }

        [Theory]
        [InlineData(45)]
        [InlineData(120)]
        public void CommandLineExamples_WrapEachCommandWithoutJoiningTheNextCommand(int width) {
            const string firstCommand = "atlas inspect --source saved-inventory --include services --include certificates";
            string[] lines = Render(firstCommand + "\natlas export --output report.html", width);
            int first = Array.FindIndex(lines, line => line.StartsWith("  atlas inspect", StringComparison.Ordinal));
            int second = Array.IndexOf(lines, "  atlas export --output report.html");
            Assert.True(first >= 0 && second > first);
            Assert.Equal(firstCommand.Replace(" ", string.Empty),
                string.Concat(lines.Skip(first).Take(second - first)).Replace(" ", string.Empty));
            Assert.All(lines, line => Assert.True(line.Length <= width));
        }

        private static string[] Render(string code, int width) {
            HelpTopic topic = new HelpTopic("atlas") {
                CommandLine = new CommandLineHelp { Usage = "atlas [command]" }
            };
            topic.Examples.Add(new HelpExample(null, code));
            string text = Terminal.RenderText(new HelpView(topic),
                TerminalProfile.Fixed(width, 100, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            return text.Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd()).ToArray();
        }
    }
}
