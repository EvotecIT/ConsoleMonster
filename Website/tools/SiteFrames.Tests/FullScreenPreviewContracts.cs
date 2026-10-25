using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The published preview must retain the real in-app action and transport evidence.</summary>
    public sealed class FullScreenPreviewContracts {
        [Theory]
        [InlineData("service-browser.cast", 120, 0)]
        [InlineData("service-browser-ssh.cast", 100, 2)]
        public void Capture_KeepsConfirmationOutputAndTransportEvents(string name, int width, int resizes) {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            Assert.NotNull(root);
            string path = Path.Combine(root!.FullName, "Website", "captures", "fullscreen", name);
            string[] lines = File.ReadAllLines(path);
            JsonObject header = (JsonObject)JsonNode.Parse(lines[0])!;
            Assert.Equal(width, header["width"]!.GetValue<int>());
            Assert.Equal("[REDACTED]", header["consolemonster"]!["host"]!.GetValue<string>());
            JsonArray[] events = lines.Skip(1).Select(line => (JsonArray)JsonNode.Parse(line)!).ToArray();
            Assert.Equal(resizes, events.Count(item => item[1]!.GetValue<string>() == "r"));
            string output = string.Concat(events.Where(item => item[1]!.GetValue<string>() == "o").Select(item => item[2]!.GetValue<string>()));
            Assert.Contains("Restart the selected services?", output);
            Assert.Contains("Restart: completed", output);
            Assert.Contains("\u001b[?1006h", output);
            Assert.Contains("\u001b[?1049h", output);
            SessionRecording recording = SessionRecording.Read(path);
            Assert.Equal(4, recording.Chapters.Count);
            string completed = ConsoleExporter.ToText(recording.ChapterFrame(3));
            Assert.Contains("Service browser", completed);
            Assert.Contains("Restart: completed", completed);
            Assert.Contains("WinRM", completed);
        }
    }
}
