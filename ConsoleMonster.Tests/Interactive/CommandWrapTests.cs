using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Interactive;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// The equivalent command wraps at argument boundaries: a parameter stays with its value, a list too wide for a line
    /// breaks after its commas (never inside quotes), continuation lines are indented, and a token is cut only when it alone
    /// is wider than the line.
    /// </summary>
    public sealed class CommandWrapTests {
        private const string Check = "Invoke-HealthCheck -Domain corp.example.net -ComputerName APP01,APP02,DB01 -Check SecureChannel,TimeOffset,ForwardRecord,PendingReboot,DiskSpace -ThrottleLimit 6";

        private static void FitsAndKeepsWords(IReadOnlyList<string> lines, int width, string command) {
            foreach (string line in lines) {
                Assert.True(DisplayWidth.Measure(line) <= width, "Line wider than " + width + ": '" + line + "'");
            }
            // Joining the lines without their indents and line breaks gives back every word of the command.
            string joined = string.Join(" ", lines.Select(l => l.Trim()).ToArray()).Replace(", ", ",");
            Assert.Equal(command, joined);
        }

        [Fact]
        public void Wrap_LongList_BreaksAfterCommasWithIndentedContinuationLines() {
            IReadOnlyList<string> lines = CommandLine.Wrap(Check, 30, 2);
            Assert.Equal(new[] {
                "Invoke-HealthCheck",
                "  -Domain corp.example.net",
                "  -ComputerName APP01,APP02,",
                "    DB01",
                "  -Check SecureChannel,",
                "    TimeOffset,ForwardRecord,",
                "    PendingReboot,DiskSpace",
                "  -ThrottleLimit 6"
            }, lines.ToArray());
            FitsAndKeepsWords(lines, 30, Check);
        }

        [Theory]
        [InlineData(24)]
        [InlineData(30)]
        [InlineData(41)]
        [InlineData(57)]
        [InlineData(80)]
        public void Wrap_AnyWidth_NeverSplitsAWordThatFitsALine(int width) {
            IReadOnlyList<string> lines = CommandLine.Wrap(Check, width, 2);
            FitsAndKeepsWords(lines, width, Check);
            foreach (string word in new[] { "SecureChannel,", "TimeOffset,", "ForwardRecord,", "PendingReboot,", "DiskSpace", "corp.example.net" }) {
                Assert.Contains(lines, l => l.Contains(word));
            }
            Assert.All(lines.Skip(1), l => Assert.StartsWith("  ", l));
        }

        [Fact]
        public void Wrap_QuotedItemWithCommaAndSpace_StaysInOnePiece() {
            string command = "Set-Label -Name 'north, east','far west',south -Force";
            IReadOnlyList<string> lines = CommandLine.Wrap(command, 18, 2);
            Assert.Equal(new[] { "Set-Label", "  -Name", "    'north, east',", "    'far west',", "    south", "  -Force" }, lines.ToArray());
        }

        [Fact]
        public void Wrap_TokenWiderThanALine_IsCutOnlyThen() {
            string command = "Copy-Report -Path C:\\Reports\\Quarterly\\Regional\\Summary.html -Open";
            IReadOnlyList<string> lines = CommandLine.Wrap(command, 20, 2);
            Assert.Equal(new[] { "Copy-Report", "  -Path", "    C:\\Reports\\Quart", "    erly\\Regional\\Su", "    mmary.html", "  -Open" }, lines.ToArray());
        }

        [Fact]
        public void Wrap_WindowsStyleList_BreaksAfterCommas() {
            string command = "tool /Server:alpha,bravo,charlie,delta /Quiet";
            IReadOnlyList<string> lines = CommandLine.Wrap(command, 16, 2);
            Assert.Equal(new[] { "tool", "  /Server:alpha,", "    bravo,", "    charlie,", "    delta", "  /Quiet" }, lines.ToArray());
        }

        [Fact]
        public void Wrap_WithoutIndent_KeepsTheEarlierLayoutForArgumentsThatFit() {
            IReadOnlyList<string> lines = CommandLine.Wrap("Install-Agent -Path 'C:\\Program Files\\Agent' -Edition pro -Site waw,ber", 32);
            Assert.Equal(new[] { "Install-Agent", "-Path 'C:\\Program Files\\Agent'", "-Edition pro -Site waw,ber" }, lines.ToArray());
        }

        [Fact]
        public void ReviewPage_CommandLongerThanTheBox_EndsWithAnEllipsisLine() {
            Form form = new Form("Health check") { Command = "Invoke-HealthCheck" };
            string[] names = Enumerable.Range(1, 40).Select(i => "Check" + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            ChoiceField checks = form.Page("Checks").MultiSelect("Checks", "Checks", names.Select(c => new Choice(c)));
            checks.ParameterName = "Check";
            checks.Value = names.Cast<object?>().ToList();
            RecordingSurface surface = new RecordingSurface(46, 30, false);
            PromptHarness.Guard(surface, () => form.Show(new FormOptions { Interactive = PromptHarness.Options("PageDown Escape Escape", surface) }));
            string[] rows = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Equivalent command")).Split('\n');
            int top = System.Array.FindIndex(rows, r => r.Contains("Equivalent command"));
            int bottom = System.Array.FindIndex(rows, top, r => r.Contains("╰"));
            string[] box = rows.Skip(top + 1).Take(bottom - top - 1).Select(r => r.Trim(' ', '│')).ToArray();
            Assert.Equal(6, box.Length);
            Assert.Equal("…", box[5]);
            Assert.StartsWith("Invoke-HealthCheck", box[0]);
        }

        [Fact]
        public void ReviewPage_NarrowWidth_WrapsTheCommandBetweenArguments() {
            Form form = new Form("Health check") { Command = "Invoke-HealthCheck" };
            FormPage page = form.Page("Checks");
            string[] names = { "SecureChannel", "TimeOffset", "ForwardRecord", "PendingReboot", "DiskSpace" };
            ChoiceField checks = page.MultiSelect("Checks", "Checks", names.Select(c => new Choice(c)));
            checks.ParameterName = "Check";
            checks.Value = names.Cast<object?>().ToList();
            string[] hosts = { "APP01", "APP02", "DB01" };
            ChoiceField servers = page.MultiSelect("Servers", "Servers", hosts.Select(s => new Choice(s)));
            servers.ParameterName = "ComputerName";
            servers.Value = hosts.Cast<object?>().ToList();
            RecordingSurface surface = new RecordingSurface(46, 26, false);
            PromptHarness.Guard(surface, () => form.Show(new FormOptions { Interactive = PromptHarness.Options("PageDown Escape Escape", surface) }));
            string review = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Equivalent command"));
            Assert.Contains("PendingReboot,", review);
            Assert.DoesNotContain("Pend\n", review.Replace(" │", string.Empty).Replace("│", string.Empty));
            Golden.Match("interactive/form.review.command-wrap.txt", review);
        }
    }
}
