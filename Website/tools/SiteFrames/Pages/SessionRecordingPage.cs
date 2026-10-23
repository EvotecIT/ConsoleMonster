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
            string cast = Path.Combine(site.Captures, "recording", "session.cast");
            if (!File.Exists(cast)) { throw new FileNotFoundException("Run Website/examples/recording/record-session.ps1 and save its cast under captures/recording.", cast); }
            SessionRecording recording = SessionRecording.Read(cast);
            site.Write(site.Static("examples/recording/TypedSession.cs"), Site.ReadText(Path.Combine(site.Website, "examples", "recording", "TypedSession.cs")));
            site.Write(site.Static("examples/recording/record-session.ps1"), Site.ReadText(Path.Combine(site.Website, "examples", "recording", "record-session.ps1")));
            RecordingExportOptions options = new RecordingExportOptions { Theme = ExportTheme.Dark, WindowChrome = true, Title = "Console session" };
            string data = recording.ExportPlayerData(options);
            List<TimedFrame> frames = new List<TimedFrame>();
            foreach (RecordedFrame frame in recording.CaptureFrames(options)) { frames.Add(new TimedFrame(frame.Start, frame.Frame)); }
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
            site.Write(site.Static("recordings/session.md"), recording.Export(ExportFormat.Markdown));
            foreach (ExportTheme theme in new[] { ExportTheme.Dark, ExportTheme.Light }) {
                site.Write(site.Static("recordings/session-final-" + theme.ToString().ToLowerInvariant() + ".svg"), recording.ExportFinalSvg(new ExportOptions { Theme = theme, WindowChrome = true, Title = "Console session" }));
            }
            JsonObject entry = new JsonObject {
                ["title"] = "Console session", ["url"] = "/replays/session-recording.json",
                ["start"] = duration, ["poster"] = site.Frame(recording.FinalFrame(), null),
                ["powershellRepo"] = "Website/examples/recording/record-session.ps1"
            };
            site.WriteJson(site.Data("session-recording"), new JsonObject { ["session"] = entry });
        }
    }
}
