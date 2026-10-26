using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SiteFrames.Capture {
    /// <summary>Producer evidence for comparing terminal cells independently of transport packet boundaries.</summary>
    internal static class CaptureRun {
        private static readonly ConcurrentDictionary<string, (int Width, int Height)> Sizes = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, string> Screens = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, bool> Produced = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, (int Width, int Height, string Hash)> PathSizes = new(StringComparer.Ordinal);
        internal static void Record(string path, string? screen = null) {
            string full = Path.GetFullPath(path);
            Produced[full] = true;
            if (screen is "dark" or "light") { Screens[full] = screen; }
        }
        internal static void Record(string path, int width, int height, string? screen = null) {
            string full = Path.GetFullPath(path);
            PathSizes[full] = (width, height, Hash(File.ReadAllBytes(full)));
            Record(full, screen);
        }
        internal static void Reset() { Sizes.Clear(); Screens.Clear(); Produced.Clear(); PathSizes.Clear(); }
        internal static string Platform => OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        internal static void Observe(byte[] bytes, int width, int height) {
            string hash = Hash(bytes);
            Sizes.AddOrUpdate(hash, (width, height), (_, previous) => previous == (width, height) ? previous : (-1, -1));
        }

        /// <summary>Writes only this invocation's production evidence, including failed/partial production when requested.</summary>
        internal static void Write(Site site, bool complete = true, string? area = null) {
            var files = new JsonObject();
            foreach (string path in Produced.Keys.OrderBy(p => p, StringComparer.Ordinal)) {
                string relative = Path.GetRelativePath(site.Captures, path).Replace('\\', '/');
                var metadata = new JsonObject {
                    ["platform"] = CapturePlatforms.IsPs5(relative) ? "windows" : Platform,
                    ["category"] = Category(relative)
                };
                files[relative] = metadata;
                try {
                    string extension = Path.GetExtension(path);
                    if (extension is not (".ansi" or ".cast" or ".txt")) { throw new InvalidDataException("Unsupported produced capture type: " + extension); }
                    string hash = Hash(File.ReadAllBytes(path));
                    metadata["sha256"] = hash;
                    if (Screens.TryGetValue(path, out string? screen)) { metadata["screen"] = screen; }
                    Sizes.TryGetValue(hash, out var size);
                    if (PathSizes.TryGetValue(path, out var observed)) {
                        if (observed.Hash != hash) { throw new InvalidDataException("Capture changed after its producer recorded geometry."); }
                        size = (observed.Width, observed.Height);
                    }
                    if (extension == ".cast") {
                        using var lines = File.ReadLines(path).GetEnumerator();
                        if (!lines.MoveNext()) { throw new InvalidDataException("Missing cast header."); }
                        JsonNode header = JsonNode.Parse(lines.Current) ?? throw new InvalidDataException("Missing cast header.");
                        size = (header["width"]!.GetValue<int>(), header["height"]!.GetValue<int>());
                    }
                    metadata["width"] = size.Width;
                    metadata["height"] = size.Height;
                    if (size.Width == -1) { throw new InvalidDataException("Identical capture bytes were observed at different terminal sizes; geometry needs path-bound evidence."); }
                    if (size.Width <= 0 || size.Height <= 0) { throw new InvalidDataException("Terminal geometry was not observed."); }
                } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException
                    or ArgumentException or NullReferenceException or FormatException or OverflowException) {
                    metadata["error"] = ex.Message;
                    complete = false;
                }
            }
            var filters = new JsonObject();
            foreach (string name in new[] { "SITEFRAMES_DEMOS", "SITEFRAMES_LIVE", "SITEFRAMES_GALLERY" }) {
                string? filter = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(filter)) { filters[name] = filter; }
            }
            site.WriteJson(Path.Combine(site.Captures, "run.json"), new JsonObject {
                ["version"] = 1, ["producer"] = Platform, ["complete"] = complete,
                ["scope"] = "produced-only", ["requestedSet"] = area ?? "all", ["filters"] = filters,
                ["allSetsCompleted"] = complete && area == null && filters.Count == 0, ["files"] = files
            });
        }

        internal static string Category(string path) => CapturePlatforms.LogicalName(path) switch {
            "benchmarks/benchmarks.cast" or "live/stress.cast" => "measurements",
            "docs/profile.ansi" or "docs/profile-detected.ansi" or "docs/profile-ci.ansi" => "terminal-profile",
            _ => "neutral"
        };
    }
}
