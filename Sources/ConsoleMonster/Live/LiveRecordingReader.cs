using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ConsoleMonster.Live {
    /// <summary>Reads version-one JSON-lines event recordings without external dependencies.</summary>
    public static class LiveRecordingReader {
        /// <summary>Reads events, skipping blank lines and the optional recording header.</summary>
        /// <param name="reader">Borrowed input reader.</param>
        /// <returns>Events in recording order. Malformed input raises FormatException with its line number.</returns>
        public static IEnumerable<LiveEvent> Read(TextReader reader) {
            if (reader == null) { throw new ArgumentNullException(nameof(reader)); }
            string? line;
            int number = 0;
            while ((line = reader.ReadLine()) != null) {
                number++;
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                LiveEvent? liveEvent;
                try { liveEvent = Parse(line); }
                catch (FormatException ex) { throw new FormatException("Malformed recording at line " + number.ToString(CultureInfo.InvariantCulture) + ": " + ex.Message, ex); }
                if (liveEvent != null) { yield return liveEvent; }
            }
        }

        private static LiveEvent? Parse(string line) {
            RecordingJson parser = new RecordingJson(line);
            Dictionary<string, string?> fields = parser.ReadObject(out HashSet<string> strings);
            if (fields.ContainsKey("consolemonster")) {
                if (GetString(fields, strings, "consolemonster", true) != "recording" || GetNumber(fields, strings, "version") != 1) {
                    throw new FormatException("Unsupported recording header.");
                }
                return null;
            }
            long time = 0;
            if (fields.TryGetValue("t", out string? timestamp)) {
                if (strings.Contains("t") || !long.TryParse(timestamp, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out time)) {
                    throw new FormatException("Invalid event timestamp.");
                }
            }
            return new LiveEvent(GetString(fields, strings, "target", true)!, GetString(fields, strings, "op", true)!,
                GetString(fields, strings, "key"), GetString(fields, strings, "text"), GetString(fields, strings, "state"),
                GetNumber(fields, strings, "value"), GetNumber(fields, strings, "total"), time);
        }

        private static string? GetString(Dictionary<string, string?> fields, HashSet<string> strings, string name, bool required = false) {
            fields.TryGetValue(name, out string? value);
            if ((required && value == null) || (value != null && !strings.Contains(name))) {
                throw new FormatException("Expected string field '" + name + "'.");
            }
            return value;
        }

        private static double GetNumber(Dictionary<string, string?> fields, HashSet<string> strings, string name) {
            if (!fields.TryGetValue(name, out string? value)) { return 0; }
            if (strings.Contains(name) || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                || double.IsNaN(result) || double.IsInfinity(result)) {
                throw new FormatException("Expected finite number field '" + name + "'.");
            }
            return result;
        }
    }
}
