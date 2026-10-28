using System;
using System.Collections.Generic;
using System.IO;

namespace SiteFrames.Capture {
    /// <summary>Explicit ownership of host-dependent captures. Unqualified historical captures are Windows receipts.</summary>
    internal static class CapturePlatforms {
        internal static bool Capturing { get; set; }
        internal static readonly string[] All = { "windows", "macos", "linux" };

        internal static string LogicalName(string name) {
            foreach (string platform in All) {
                string extension = Path.GetExtension(name);
                if (name.EndsWith("." + platform + extension, StringComparison.Ordinal)) {
                    return name[..^(platform.Length + extension.Length + 1)] + extension;
                }
            }
            return name;
        }

        internal static string Tagged(string path, string platform) => Path.ChangeExtension(path, platform + Path.GetExtension(path));

        internal static bool IsPs5(string path) {
            string logical = LogicalName(path).Replace('\\', '/');
            return Path.GetFileNameWithoutExtension(logical).EndsWith(".ps5", StringComparison.Ordinal)
                || logical == "live/pipeline-ps5.cast"
                || logical.StartsWith("tails/ps5-", StringComparison.Ordinal)
                || (logical.StartsWith("help-formatting/", StringComparison.Ordinal) && logical.Contains("-ps5-", StringComparison.Ordinal));
        }

        internal static string? Tag(string path) {
            foreach (string platform in All) {
                if (path.EndsWith("." + platform + Path.GetExtension(path), StringComparison.Ordinal)) { return platform; }
            }
            return null;
        }

        internal static string PathFor(Site site, string path) {
            string relative = Path.GetRelativePath(site.Captures, path).Replace('\\', '/');
            if (CaptureRun.Category(relative) == "neutral" || IsPs5(relative)) { return path; }
            if (Capturing) { return Tagged(path, CaptureRun.Platform); }
            // Build output never depends on the OS running render. Existing Windows receipts keep their canonical
            // presentation; a requested platform is resolved explicitly by the consuming page.
            return File.Exists(Tagged(path, "windows")) ? Tagged(path, "windows") : path;
        }

        internal static IEnumerable<(string Platform, string Path)> Variants(string path) {
            foreach (string platform in All) {
                string candidate = Tagged(path, platform);
                if (File.Exists(candidate)) { yield return (platform, candidate); }
                else if (platform == "windows" && File.Exists(path)) { yield return (platform, path); }
            }
        }

        internal static string Label(string platform) => platform switch { "macos" => "macOS", "windows" => "Windows", "linux" => "Linux", _ => platform };
    }
}
