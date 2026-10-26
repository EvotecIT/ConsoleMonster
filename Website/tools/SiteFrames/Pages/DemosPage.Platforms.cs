using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames.Capture;
using SiteFrames.Replay;

namespace SiteFrames.Pages {
    internal static partial class DemosPage {
        private static void AddPlatformVariants(Site site, Demo demo, JsonObject entry, System.Collections.Generic.List<string> problems) {
            string logical = Path.Combine(site.Captures, "demos", demo.Id + ".cast");
            if (CaptureRun.Category("demos/" + demo.Id + ".cast") == "neutral") { return; }
            var variants = new JsonObject();
            string primary = CastPath(site, demo.Id);
            foreach ((string platform, string path) in CapturePlatforms.Variants(logical)) {
                var variant = new JsonObject { ["platform"] = platform, ["platformLabel"] = CapturePlatforms.Label(platform),
                    ["title"] = demo.Title, ["terminal"] = entry["terminal"]!.DeepClone() };
                if (path == primary) {
                    foreach (string key in new[] { "w", "h", "frame", "url", "poster", "frames", "duration", "fps", "start", "hero", "bytes" }) {
                        if (entry[key] != null) { variant[key] = entry[key]!.DeepClone(); }
                    }
                    entry["platform"] = platform;
                    entry["platformLabel"] = CapturePlatforms.Label(platform);
                } else {
                    string cast = File.ReadAllText(path);
                    string? source = Source(site, demo);
                    if (source != null && Replays.Source(cast) != Replays.Fingerprint(source, CapturePins.Policy)) { problems.Add(demo.Id + " (" + platform + ") is stale; capture it on " + CapturePlatforms.Label(platform) + "."); }
                    var frames = Replays.FromCast(cast, demo.Fps, false, out var scrollback, out _, out _);
                    if (demo.EndsOn != null) {
                        int end = frames.FindLastIndex(frame => Shows(frame.Frame, demo.EndsOn));
                        if (end < 0) { problems.Add(demo.Id + " (" + platform + ") has no closing screen."); }
                        else { frames.RemoveRange(end + 1, frames.Count - end - 1); }
                    }
                    frames = ReplayFit.Trim(frames);
                    if (demo.Static) { variant["frame"] = site.Frame(Site.Crop(scrollback), null); }
                    else if (frames.Count == 0) { problems.Add(demo.Id + " (" + platform + ") has no replay frames."); }
                    else {
                        string data = PlayerData.Write(site, frames, demo.Title, demo.Fps, 3000, out long duration, out int count);
                        string url = "replays/demo-" + demo.Id + "-" + platform + ".json";
                        site.Write(site.Static(url), data);
                        long posterTime = LivePage.Poster(site, frames, demo.EndsOn != null ? 1 : demo.PosterShare, out string poster, out _);
                        int width = frames[0].Frame.Width, height = frames[0].Frame.Height;
                        variant["url"] = "/" + url; variant["w"] = width; variant["h"] = height;
                        variant["duration"] = duration; variant["frames"] = count; variant["fps"] = demo.Fps;
                        variant["start"] = demo.EndsOn != null || demo.PosterShare >= 1 ? duration : posterTime;
                        variant["hero"] = ReplayFit.Hero(frames);
                        variant["poster"] = new JsonObject { ["w"] = width, ["h"] = height, ["dark"] = poster };
                    }
                }
                variants[platform] = variant;
            }
            entry["platforms"] = variants;
        }
    }
}
