using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SiteFrames.Capture {
    /// <summary>Reviewed capture metadata; undeclared output always retains strict repeat and cell identity.</summary>
    internal static class CaptureClasses {
        internal const string FileName = "classes.json";
        internal sealed record LiveMeasured(string Reason, int MinFrames, int MaxFrames, double MinDurationSeconds, double MaxDurationSeconds) {
            internal JsonObject Json() => new() {
                ["class"] = "live-measured", ["reason"] = Reason,
                ["minFrames"] = MinFrames, ["maxFrames"] = MaxFrames,
                ["minDurationSeconds"] = MinDurationSeconds, ["maxDurationSeconds"] = MaxDurationSeconds
            };
        }

        internal static Dictionary<string, LiveMeasured> Read(string root) {
            string path = Path.Combine(root, FileName);
            var result = new Dictionary<string, LiveMeasured>(StringComparer.Ordinal);
            if (!File.Exists(path)) { return result; }
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { throw new InvalidDataException("Capture classes cannot be a symbolic link."); }
            JsonObject document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            if (document["version"]?.GetValue<int>() != 1) { throw new InvalidDataException("Unsupported capture classes version."); }
            foreach (var entry in document["captures"]!.AsObject()) {
                string name = entry.Key;
                if (name != CapturePlatforms.LogicalName(name) || !name.EndsWith(".cast", StringComparison.Ordinal)
                    || Path.IsPathRooted(name) || name.Contains('\\') || name.Contains(':')
                    || Array.Exists(name.Split('/'), part => part is "" or "." or "..")) {
                    throw new InvalidDataException("Capture class needs a canonical logical cast path: " + name);
                }
                result.Add(name, Parse(entry.Value!.AsObject()));
            }
            return result;
        }

        internal static LiveMeasured Parse(JsonObject value) {
            if (value["class"]?.GetValue<string>() != "live-measured") { throw new InvalidDataException("Unknown capture class."); }
            string reason = value["reason"]!.GetValue<string>();
            int min = value["minFrames"]!.GetValue<int>(), max = value["maxFrames"]!.GetValue<int>();
            double start = value["minDurationSeconds"]!.GetValue<double>(), end = value["maxDurationSeconds"]!.GetValue<double>();
            if (string.IsNullOrWhiteSpace(reason) || min < 2 || max < min || max > 100_000
                || !double.IsFinite(start) || !double.IsFinite(end) || start <= 0 || end < start || end > 600) {
                throw new InvalidDataException("Live-measured captures need a reason and finite, nonempty frame/duration bounds.");
            }
            return new LiveMeasured(reason, min, max, start, end);
        }
    }
}
