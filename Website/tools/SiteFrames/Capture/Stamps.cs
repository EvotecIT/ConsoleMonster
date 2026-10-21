using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames.Replay;

namespace SiteFrames.Capture {
    /// <summary>
    /// What made each raw console capture (<c>captures/**/*.ansi</c>): <c>captures/sources.json</c> maps a capture to a
    /// fingerprint of the script it ran, the environment and the size. The capture command writes the entry, and the render
    /// reports a capture whose script or settings changed since, so a page never shows output from an older script.
    /// (Recordings carry the same fingerprint in their own header; see <see cref="Replays.Stamp"/>.)
    /// </summary>
    internal static class Stamps {
        private static string ManifestPath(Site site) => Path.Combine(site.Captures, "sources.json");

        /// <summary>The fingerprint of a script run: its text, the environment it ran with and anything else that shapes it.</summary>
        public static string Of(string script, IDictionary<string, string?> environment, params string[] extra) {
            List<string> parts = new List<string> { Site.ReadText(script) };
            parts.AddRange(environment.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).Select(e => e.Key + "=" + e.Value));
            parts.AddRange(extra);
            return Replays.Fingerprint(parts.ToArray());
        }

        /// <summary>Records what made a capture.</summary>
        public static void Write(Site site, string capture, string fingerprint) {
            JsonObject manifest = Read(site);
            manifest[Key(site, capture)] = fingerprint;
            SortedDictionary<string, string> sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonNode?> entry in manifest) {
                sorted[entry.Key] = entry.Value!.GetValue<string>();
            }
            JsonObject output = new JsonObject();
            foreach (KeyValuePair<string, string> entry in sorted) {
                output[entry.Key] = entry.Value;
            }
            File.WriteAllText(ManifestPath(site), output.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n", new System.Text.UTF8Encoding(false));
        }

        /// <summary>A problem when a capture was made from something else than <paramref name="fingerprint"/>, or null.</summary>
        public static string? Check(Site site, string capture, string fingerprint, string area) {
            string key = Key(site, capture);
            string? recorded = Read(site)[key]?.GetValue<string>();
            return recorded == fingerprint ? null : key + " was captured from another version of its script or settings; run the capture command for the " + area + " area.";
        }

        private static JsonObject Read(Site site) {
            string path = ManifestPath(site);
            return File.Exists(path) ? (JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject()) : new JsonObject();
        }

        private static string Key(Site site, string capture) => Path.GetRelativePath(site.Captures, capture).Replace('\\', '/');
    }
}
