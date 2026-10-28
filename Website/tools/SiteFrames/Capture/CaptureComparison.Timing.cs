using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SiteFrames.Capture {
    internal static partial class CaptureComparison {
        // Canonical casts use 50 ms transport steps. Additional gaps are scripted playback holds, not arrival jitter.
        private static string ReadingHolds(string text, string extension) {
            if (extension != ".cast") { return string.Empty; }
            using var lines = new StringReader(text);
            using var header = JsonDocument.Parse(lines.ReadLine()!);
            if (!header.RootElement.TryGetProperty("cm_timing", out var timing) || timing.GetString() != "logical-50ms-with-holds") { return string.Empty; }
            var result = new StringBuilder();
            double previous = 0;
            int index = 0;
            string? line;
            while ((line = lines.ReadLine()) != null) {
                if (line.Length == 0) { continue; }
                using var document = JsonDocument.Parse(line);
                var entry = document.RootElement;
                if (entry[1].GetString() != "o") { continue; }
                double current = entry[0].GetDouble() * 1000;
                double gap = Math.Round(current - previous - (index == 0 ? 0 : 50), 3);
                if (gap != 0) { result.Append(index).Append(':').Append(gap.ToString("0.###", CultureInfo.InvariantCulture)).Append(';'); }
                previous = current;
                index++;
            }
            return result.ToString();
        }
    }
}
