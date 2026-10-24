using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Acceptance checks on the real session published with the recording page.</summary>
    public sealed class SessionDemoContracts {
        [Fact]
        public void DemoCapture_ContainsPacedCommandsProgressAndPortableSyntheticData() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            Assert.NotNull(root);
            string path = Path.Combine(root!.FullName, "Website", "captures", "recording", "session.cast");
            string[] lines = File.ReadAllLines(path);
            JsonObject header = (JsonObject)JsonNode.Parse(lines[0])!;
            Assert.Equal(2, header["version"]!.GetValue<int>());
            Assert.InRange(header["width"]!.GetValue<int>(), 100, 120);
            Assert.Contains(header["consolemonster"]!["mode"]!.GetValue<string>(), new[] { "ConPTY", "openpty" });
            Assert.Equal("[REDACTED]", header["consolemonster"]!["host"]!.GetValue<string>());
            JsonArray[] events = lines.Skip(1).Select(line => (JsonArray)JsonNode.Parse(line)!).ToArray();
            // Hidden setup is excluded from the timeline; chapters and paced echo describe the visible demo.
            JsonArray[] markers = events.Where(item => item[1]!.GetValue<string>() == "m").ToArray();
            Assert.Equal(new[] { "Inspect", "Build", "Report", "Errors and configuration" }, markers.Select(item => item[2]!.GetValue<string>()));
            double inspect = markers[0][0]!.GetValue<double>(), build = markers[1][0]!.GetValue<double>();
            for (int index = 1; index < markers.Length; index++) {
                Assert.True(markers[index - 1][0]!.GetValue<double>() < markers[index][0]!.GetValue<double>());
            }
            Assert.True(markers.Last()[0]!.GetValue<double>() < events.Last()[0]!.GetValue<double>());
            Assert.True(events.Count(item => item[1]!.GetValue<string>() == "o" && item[0]!.GetValue<double>() > inspect && item[0]!.GetValue<double>() < build) > 15,
                "The command must arrive as paced terminal echo, rather than one echoed line.");
            string raw = string.Concat(events.Where(item => item[1]!.GetValue<string>() == "o").Select(item => item[2]!.GetValue<string>()));
            Assert.Contains("Step 1 of 10", raw);
            Assert.Contains("Step 10 of 10", raw);
            Assert.Contains("\u001b[", raw);
            Assert.DoesNotContain("D:\\Dev\\", raw);
            string transcript = SessionRecording.Read(path).Export(ExportFormat.Text);
            string[] transcriptLines = transcript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Assert.Contains("Build report", transcriptLines);
            Assert.Contains("PS demo> git log --oneline --graph --color -5", transcriptLines);
            Assert.Matches(@"Cannot find path '[^\r\n]*[/\\]missing\.json' because it does not exist\.", transcript);
            Assert.DoesNotContain("Cannot find drive", transcript);
            foreach (string result in new[] { "✓ Passed", "24 passed", "✓ Ready" }) { Assert.Contains(result, transcript); }
            foreach (string expected in new[] { "PS demo>", "git log --oneline --graph --color -5", "Document local recording", "Build report", "24 passed", "Cannot find path", "Show-DemoToken $env:API_TOKEN", "API_TOKEN=demo-token (synthetic)" }) {
                Assert.Contains(expected, transcript);
            }
        }
    }
}
