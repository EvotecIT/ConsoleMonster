using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ConsoleMonster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Vt;

namespace SiteFrames.Replay {
    /// <summary>Exports recorded static output or complete typed steps, independent of native repaint packet boundaries.</summary>
    internal static class RecordedSteps {
        internal static string ToCast(string nativeRecording, string title, bool still) {
            SessionRecording session = SessionRecording.Read(new StringReader(nativeRecording));
            var profile = TerminalProfile.Fixed(session.Width, session.Height).WithCulture(Site.Culture).WithHyperlinks(true);
            using var writer = new StringTerminalWriter();
            using var output = Terminal.CreateOutput(profile, writer);
            if (still) {
                CellBuffer transcript = Prepare(session.Transcript());
                int rows = transcript.Height;
                while (rows > 1 && FrameHtml.ContentEnd(transcript, rows - 1, ColorDepth.TrueColor) == 0) { rows--; }
                var trimmed = new CellBuffer(transcript.Width, rows);
                trimmed.Blit(transcript, 0, 0, transcript.Width, rows, 0, 0);
                output.WriteBlock(trimmed);
                return Replays.ToCast(new[] { new OutputChunk(0, Encoding.UTF8.GetBytes(writer.ToString())) }, session.Width, session.Height, title);
            }
            var markers = session.Chapters;
            if (markers.Count < 2 || markers[markers.Count - 1].Label != "site:end") { throw new InvalidDataException("Typed capture has no complete step boundaries."); }
            var cast = new StringBuilder("{\"version\":2,\"width\":" + session.Width + ",\"height\":" + session.Height +
                ",\"title\":" + JsonSerializer.Serialize(title) + ",\"cm_timing\":\"logical-steps-v1\"}\n");
            double time = 0;
            for (int step = 0; step < markers.Count - 1; step++) {
                double hold = markers[step].Label switch {
                    "site:comment" => 0.6, "site:prompt" => 0.4, "site:output" => 1.2,
                    _ => throw new InvalidDataException("Unknown typed capture step: " + markers[step].Label)
                };
                CellBuffer frame = Prepare(session.ChapterFrame(step));
                Cell corner = frame[frame.Width - 1, frame.Height - 1];
                if (corner.Glyph != " " || corner.Style.Decoration != TextDecoration.None || corner.Style.Link != null || !corner.Style.BackgroundColor.IsDefault) {
                    throw new InvalidDataException("Typed capture uses the reserved terminal bottom-right cell.");
                }
                // RenderFrame has no alternate-screen lifecycle here: the final recorded step remains visible.
                writer.Clear();
                output.RenderFrame(frame);
                cast.Append('[').Append(time.ToString("0.000", CultureInfo.InvariantCulture)).Append(",\"o\",")
                    .Append(JsonSerializer.Serialize(writer.ToString())).Append("]\n");
                time += hold;
            }
            return cast.ToString();
        }

        private static CellBuffer Prepare(CellBuffer frame) {
            foreach (string error in CaptureComparison.Quality(frame)) { throw new InvalidDataException("Recorded step: " + error); }
            return FrameDefaults.ForRecording(frame, Screen.Dark);
        }
    }
}
