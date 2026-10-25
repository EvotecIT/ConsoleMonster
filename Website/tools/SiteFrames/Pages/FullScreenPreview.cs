using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Captures the paced raster preview once; render verifies and copies its platform-specific pixels.</summary>
    internal static class FullScreenPreview {
        internal static string GifPath(Site site) => Path.Combine(site.Captures, "fullscreen", "service-browser.gif");

        internal static RecordingExportOptions Playback() => new RecordingExportOptions {
            Theme = ExportTheme.Dark, WindowChrome = true, Title = "Service browser",
            FramesPerSecond = 10, HoldLastFrame = TimeSpan.FromSeconds(2)
        };

        internal static void CaptureGif(Site site) {
            string path = GifPath(site);
            Read(Path.Combine(site.Captures, "fullscreen", "service-browser.cast")).SaveGraphics(path, GraphicsFormat.Gif, new GraphicsExportOptions {
                Frame = new ExportOptions { Theme = ExportTheme.Dark, WindowChrome = true, Title = "Service browser" },
                Playback = Playback(), IdleLimit = TimeSpan.FromSeconds(1.5)
            });
            Stamps.Write(site, path, Fingerprint(site));
        }

        internal static string? CheckGif(Site site) => !File.Exists(GifPath(site))
            ? "Missing full-screen GIF; run SiteFrames capture-fullscreen-preview Website."
            : Stamps.Check(site, GifPath(site), Fingerprint(site), "fullscreen-preview (SiteFrames capture-fullscreen-preview Website)");

        // Include both the input and output bytes: a changed cast, pacing policy or replaced GIF requires an explicit export.
        private static string Fingerprint(Site site) => Stamps.Of(
            Path.Combine(site.Website, "tools", "SiteFrames", "Pages", "FullScreenPreview.cs"),
            new Dictionary<string, string?>(),
            Site.ReadText(Path.Combine(site.Captures, "fullscreen", "service-browser.cast")),
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(GifPath(site)))));

        // Keep terminal initialization, but remove startup idle from the authored preview.
        // The committed source cast remains the unedited native recording.
        internal static SessionRecording Read(string path) {
            string[] lines = File.ReadAllLines(path);
            double start = 0;
            foreach (string line in lines[1..]) {
                JsonArray entry = (JsonArray)JsonNode.Parse(line)!;
                if (entry[1]!.GetValue<string>() == "o" && entry[2]!.GetValue<string>().Contains("Service browser", StringComparison.Ordinal)) {
                    start = entry[0]!.GetValue<double>();
                    break;
                }
            }
            StringBuilder cast = new StringBuilder(lines[0]).Append('\n');
            foreach (string line in lines[1..]) {
                JsonArray entry = (JsonArray)JsonNode.Parse(line)!;
                entry[0] = Math.Max(0, entry[0]!.GetValue<double>() - start);
                cast.Append(entry.ToJsonString()).Append('\n');
            }
            return SessionRecording.Read(new StringReader(cast.ToString()));
        }
    }
}
