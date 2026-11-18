using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class CodePageTextTests {
        [Fact]
        public void RedirectedOutputOnALegacyCodePageKeepsTextThatCodePageCanEncode() {
            string text = Capture(Redirected(852));

            // The parent decodes code page 852, which holds these letters but not the box-drawing rule glyph.
            Assert.Contains("Zażółć", text);
            Assert.Contains("--", text);
            Assert.DoesNotContain("─", text);
        }

        [Fact]
        public void ExplicitAsciiStillTransliteratesText() {
            string text = Capture(Redirected(852, new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = "ascii" }));

            Assert.Contains("Zazolc", text);
            Assert.DoesNotContain("Zażółć", text);
        }

        private static TerminalProfile Redirected(int codePage, Dictionary<string, string>? variables = null) {
#if NET
            // Legacy code pages need the provider on .NET; PowerShell and most console hosts register it.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            return new CapabilityDetector(new TerminalEnvironment(variables ?? new Dictionary<string, string>(), isWindows: true,
                isOutputRedirected: true, isConsoleHandle: false).WithOutputCodePage(codePage)).Detect();
        }

        private static string Capture(TerminalProfile profile) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                output.WriteBlock(Terminal.Render(new Rule("Zażółć"), profile));
            }
            return writer.ToString();
        }
    }
}
