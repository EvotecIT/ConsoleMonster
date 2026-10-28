using System.IO;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    internal static partial class LivePage {
        private static void AddStressPlatforms(Site site, JsonObject primary) {
            var variants = new JsonObject();
            string canonical = CastPath(site, "stress");
            foreach ((string platform, string path) in CapturePlatforms.Variants(Path.Combine(site.Captures, "live", "stress.cast"))) {
                if (path == canonical) {
                    primary["platform"] = platform;
                    primary["platformLabel"] = CapturePlatforms.Label(platform);
                    continue;
                }
                JsonObject variant = FromCast(site, "stress", "C#", "dotnet run -- 12", 3, 0.45, path, "stress-" + platform);
                variant["platform"] = platform;
                variant["platformLabel"] = CapturePlatforms.Label(platform);
                variant["result"] = StressResult(site, path);
                foreach (string field in new[] { "csharp", "csharpFull", "csharpFile", "csharpFluent", "csharpFluentFull", "csharpFluentFile", "apiMap" }) {
                    if (primary[field] != null) { variant[field] = primary[field]!.DeepClone(); }
                }
                variants[platform] = variant;
            }
            primary["platforms"] = variants;
        }
    }
}
