using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        public static void Write(Site site, string capture, string fingerprint) => Locked(() => {
            WriteLocked(site, capture, fingerprint);
            CaptureRun.Record(capture);
            return true;
        });

        // One reader or writer at a time across capture and render runs, and the file is swapped in whole, so no run drops
        // another's entries or reads a file that is being replaced.
        private static T Locked<T>(Func<T> action) {
            using (Mutex mutex = new Mutex(false, @"Local\cmsite-capture-sources")) {
                try {
                    mutex.WaitOne();
                } catch (AbandonedMutexException) {
                    // The previous holder died; the file it was writing is either the old one or the new one, never half.
                }
                try {
                    return action();
                } finally {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static void WriteLocked(Site site, string capture, string fingerprint) {
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
            string path = ManifestPath(site);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllText(temporary, output.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n", new System.Text.UTF8Encoding(false));
                File.Move(temporary, path, true);
            } finally {
                if (File.Exists(temporary)) {
                    File.Delete(temporary);
                }
            }
        }

        /// <summary>A problem when a capture was made from something else than <paramref name="fingerprint"/>, or null.</summary>
        public static string? Check(Site site, string capture, string fingerprint, string area) {
            string key = Key(site, capture);
            string? recorded = Locked(() => Read(site))[key]?.GetValue<string>();
            if (recorded == null) {
                return key + " has no entry in captures/sources.json, so what made it is unknown; run the capture command for the " + area + " area.";
            }
            return recorded == fingerprint ? null : key + " was captured from another version of its script or settings; run the capture command for the " + area + " area.";
        }

        private static JsonObject Read(Site site) {
            string path = ManifestPath(site);
            return File.Exists(path) ? (JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject()) : new JsonObject();
        }

        private static string Key(Site site, string capture) => Path.GetRelativePath(site.Captures, capture).Replace('\\', '/');
    }
}
