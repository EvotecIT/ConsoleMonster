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
        public void DemoCapture_ContainsTypedCommandsProgressAndContextualRedaction() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            Assert.NotNull(root);
            string path = Path.Combine(root!.FullName, "Website", "captures", "recording", "session.cast");
            string[] lines = File.ReadAllLines(path);
            JsonObject header = (JsonObject)JsonNode.Parse(lines[0])!;
            Assert.Equal(2, header["version"]!.GetValue<int>());
            Assert.Contains(header["consolemonster"]!["mode"]!.GetValue<string>(), new[] { "ConPTY", "openpty" });
            JsonArray[] events = lines.Skip(1).Select(line => (JsonArray)JsonNode.Parse(line)!).ToArray();
            Assert.InRange(events.Last()[0]!.GetValue<double>(), 20, 30);
            JsonArray[] markers = events.Where(item => item[1]!.GetValue<string>() == "m").ToArray();
            Assert.Equal(new[] { "Inspect", "Build", "Report" }, markers.Select(item => item[2]!.GetValue<string>()));
            Assert.InRange(markers[0][0]!.GetValue<double>(), 0, 5);
            double inspect = markers[0][0]!.GetValue<double>(), build = markers[1][0]!.GetValue<double>();
            Assert.True(events.Count(item => item[1]!.GetValue<string>() == "o" && item[0]!.GetValue<double>() > inspect && item[0]!.GetValue<double>() < build) > 15,
                "The command must arrive as paced terminal echo, rather than one echoed line.");
            string raw = string.Concat(events.Where(item => item[1]!.GetValue<string>() == "o").Select(item => item[2]!.GetValue<string>()));
            Assert.Contains("Step 1 of 20", raw);
            Assert.Contains("Step 10 of 20", raw);
            Assert.Contains("\u001b[", raw);
            Assert.DoesNotContain("demo-session-only-value", raw);
            Assert.DoesNotContain("D:\\Dev\\", raw);
            string transcript = SessionRecording.Read(path).Export(ExportFormat.Text);
            foreach (string expected in new[] { "PS C:\\demo>", "git log --oneline --graph --color -5", "Document local recording", "Build report", "24 passed", "Cannot find path", "Show-DemoToken $env:API_TOKEN", "API_TOKEN=••••••• (redacted)" }) {
                Assert.Contains(expected, transcript);
            }
        }
    }
}
