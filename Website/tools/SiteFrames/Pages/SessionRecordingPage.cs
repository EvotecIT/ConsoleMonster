using System.IO;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using SiteFrames.Replay;

namespace SiteFrames.Pages {
    /// <summary>Deterministic exports from the real Invoke-ConsoleRecording demo capture.</summary>
    internal static class SessionRecordingPage {
        public static void Render(Site site) {
            RenderCommandExample(site);
            string cast = Path.Combine(site.Captures, "recording", "session.cast");
            if (!File.Exists(cast)) { throw new FileNotFoundException("Run Website/examples/recording/record-session.ps1 and save its cast under captures/recording.", cast); }
            SessionRecording recording = SessionRecording.Read(cast);
            site.Write(site.Static("examples/recording/demo.ps1"), Site.ReadText(Path.Combine(site.Website, "examples", "recording", "demo.ps1")));
            site.Write(site.Static("examples/recording/record-session.ps1"), Site.ReadText(Path.Combine(site.Website, "examples", "recording", "record-session.ps1")));
            RecordingExportOptions options = new RecordingExportOptions { Theme = ExportTheme.Dark, WindowChrome = true, Title = "Console session" };
            string data = recording.ExportPlayerData(options);
            List<TimedFrame> frames = new List<TimedFrame>();
            foreach (RecordedFrame frame in recording.CaptureFrames(options)) { frames.Add(new TimedFrame(frame.Start, frame.Frame)); }
            frames = ReplayFit.Trim(frames);
            List<(long Time, string Label)> markers = new List<(long Time, string Label)>();
            JsonObject player = (JsonObject)JsonNode.Parse(data)!;
            if (player["markers"] is JsonArray chapters) {
                foreach (JsonNode? chapter in chapters) { markers.Add((chapter![0]!.GetValue<long>(), chapter[1]!.GetValue<string>())); }
            }
            string siteData = PlayerData.Write(site, frames, "Console session", options.FramesPerSecond, 2000, out long duration, out _, markers);
            site.Write(site.Static("replays/session-recording.json"), siteData);
            site.Write(site.Static("recordings/session.cast"), recording.ToAsciicast());
            site.Write(site.Static("recordings/session.html"), recording.Export(ExportFormat.Html, options));
            site.Write(site.Static("recordings/session.svg"), recording.Export(ExportFormat.Svg, options));
            site.Write(site.Static("recordings/session.txt"), recording.Export(ExportFormat.Text));
            site.Write(site.Static("recordings/session.md"), ConsoleMonster.Markdown.MarkdownExporter.ExportRecording(recording));
            foreach (ExportTheme theme in new[] { ExportTheme.Dark, ExportTheme.Light }) {
                site.Write(site.Static("recordings/session-final-" + theme.ToString().ToLowerInvariant() + ".svg"), recording.ExportFinalSvg(new ExportOptions { Theme = theme, WindowChrome = true, Title = "Console session" }));
            }
            JsonObject entry = new JsonObject {
                ["title"] = "Console session", ["url"] = "/replays/session-recording.json",
                ["start"] = duration, ["hero"] = ReplayFit.Hero(frames),
                ["poster"] = site.Frame(frames[frames.Count - 1].Frame, null),
                ["powershellRepo"] = "Website/examples/recording/record-session.ps1"
            };
            site.WriteJson(site.Data("session-recording"), new JsonObject { ["session"] = entry });
        }

        private static void RenderCommandExample(Site site) {
            string folder = Path.Combine(site.Website, "examples", "recording");
            string script = Site.ReadText(Path.Combine(folder, "record-command.ps1"));
            string program = Site.ReadText(Path.Combine(folder, "RecordCommand.cs"));
            Capture.CSharpRunner.Compile(Path.Combine(folder, "RecordCommand.cs"));
            SessionRecording recording = SessionRecording.Read(Path.Combine(site.Captures, "recording", "command.cast"));
            var (frame, _) = Site.Fit(recording.FinalFrame(), null);
            Figures.Add("session-command", new JsonObject {
                ["title"] = "Command review", ["label"] = "A native Git command and coloured PowerShell output",
                ["meta"] = new JsonArray("Native output", "asciicast v2"), ["frame"] = site.Frame(frame, null),
                ["file"] = "record-command.ps1", ["script"] = script, ["csharp"] = program
            });
            site.Write(site.Static("examples/recording/record-command.ps1"), script);
            site.Write(site.Static("examples/recording/RecordCommand.cs"), program);
        }
    }
}
