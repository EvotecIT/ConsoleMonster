using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ConsoleMonster.Sessions;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Replay;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class PrintedReportContracts {
        [NativeEditorTheory]
        [InlineData(110, 20)]
        public void PrintedReportKeepsItsMeasuredLinesAndLogicalPlaybackDuration(int width, int height) {
            string folder = Path.Combine(Path.GetTempPath(), "cmsite-report-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string source = Path.Combine(folder, "Report.cs");
                File.WriteAllText(source, "Console.WriteLine(\"event apply: 10,276 events/s\"); Console.WriteLine(\"compose per frame: 0.844 ms\");");
                string dll = CSharpRunner.Build(source, folder);
                var chunks = CSharpRunner.RecordPrinted(dll, Array.Empty<string>(), width, height,
                    PowerShellRunner.Environment("dark"), folder, TimeSpan.FromSeconds(30));
                string cast = Replays.ToCast(chunks, width, height, "Measured report");
                SessionRecording recording = SessionRecording.Read(new StringReader(cast));
                using JsonDocument last = JsonDocument.Parse(cast.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last());
                Assert.True(last.RootElement[0].GetDouble() >= 0.05, "Printed lines collapsed into a zero-duration recording.");
                Assert.Contains("event apply: 10,276 events/s", recording.FrameAt(TimeSpan.Zero).ToPlainText());
                Assert.DoesNotContain("compose per frame", recording.FrameAt(TimeSpan.Zero).ToPlainText());
                string[] rows = recording.FinalFrame().ToPlainText().Split('\n').Select(row => row.Trim()).Where(row => row.Length > 0).ToArray();
                Assert.Equal(new[] { "event apply: 10,276 events/s", "compose per frame: 0.844 ms" }, rows);
            } finally { Site.RemoveTemporary(folder); }
        }
    }
}
