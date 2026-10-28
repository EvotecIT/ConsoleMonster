using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SiteFrames.Pages {
    internal static partial class GalleryPage {
        /// <summary>Splits actual recorder output at the chapter controls emitted by the site-only driver.</summary>
        private static string[] RecordedStates(string cast, int count) {
            var states = new string[count];
            var text = new StringBuilder();
            int current = -1;
            bool ended = false;
            using var lines = new StringReader(cast);
            lines.ReadLine();
            string? line;
            while ((line = lines.ReadLine()) != null) {
                using var document = JsonDocument.Parse(line);
                JsonElement entry = document.RootElement;
                string kind = entry[1].GetString()!, data = entry[2].GetString()!;
                if (kind == "m") {
                    if (current >= 0) { states[current] = text.ToString(); text.Clear(); }
                    if (data == "end") { ended = true; current = -1; }
                    else if (data.StartsWith("state:", StringComparison.Ordinal) && int.TryParse(data.AsSpan(6), out int state) && state >= 0 && state < count && states[state] == null) { current = state; }
                    else { throw new InvalidDataException("Unexpected gallery recording chapter: " + data); }
                } else if (kind == "o" && current >= 0) { text.Append(data); }
            }
            if (!ended || Array.Exists(states, state => string.IsNullOrEmpty(state))) { throw new InvalidDataException("Gallery recording omitted a complete state or its closing chapter."); }
            return states;
        }
    }
}
