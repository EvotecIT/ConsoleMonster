using System;
using System.Globalization;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live.Tails {
    /// <summary>Extracts optional metadata while preserving the entire input as literal text.</summary>
    public static class LogLineParser {
        /// <summary>Parses a completed line for a leading timestamp and the first recognised whole level word.</summary>
        /// <param name="line">The literal line without its terminator.</param>
        /// <returns>The unchanged text, a canonical level (info by default), an optional timestamp and a display message.</returns>
        /// <remarks>
        /// Level words use the existing <see cref="LogLevels"/> vocabulary, ignore case, and must be delimited by
        /// non-word characters (letters, numbers and underscores belong to words). Timestamp candidates are a leading
        /// bracketed value or a leading ISO date/time, optionally with a numeric offset or Z. A complete date is required;
        /// time-only and incomplete dates never acquire today's date. Dates without an offset are interpreted as UTC.
        /// Original text is never changed. The display message omits recognized leading timestamp and level fields,
        /// including brackets and adjacent whitespace, colon or pipe separators. Level words inside the body remain literal.
        /// No markup or ANSI escapes are interpreted.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
        public static TailedLogEntry Parse(string line) {
            if (line == null) { throw new ArgumentNullException(nameof(line)); }
            string level = "info";
            int levelStart = -1;
            int levelEnd = -1;
            for (int i = 0; i < line.Length; i++) {
                if (!IsWord(line[i])) { continue; }
                int start = i;
                while (i < line.Length && IsWord(line[i])) { i++; }
                if (LogLevels.TryNormalize(line.Substring(start, i - start), out level)) {
                    levelStart = start;
                    levelEnd = i;
                    break;
                }
            }
            DateTimeOffset? timestamp = ReadTimestamp(line, out int timestampEnd);
            return new TailedLogEntry(line, level, timestamp, Message(line, timestampEnd, levelStart, levelEnd));
        }

        private static bool IsWord(char value) => char.IsLetterOrDigit(value) || value == '_';

        private static string Message(string line, int timestampEnd, int levelStart, int levelEnd) {
            int start = SkipSeparators(line, timestampEnd);
            bool bracket = start < line.Length && line[start] == '[';
            int wordStart = bracket ? start + 1 : start;
            if (wordStart == levelStart && (!bracket || levelEnd < line.Length && line[levelEnd] == ']')) {
                return line.Substring(SkipSeparators(line, levelEnd + (bracket ? 1 : 0)));
            }
            return timestampEnd > 0 ? line.Substring(start) : line;
        }

        private static int SkipSeparators(string line, int start) {
            while (start < line.Length && (char.IsWhiteSpace(line[start]) || line[start] == ':' || line[start] == '|')) { start++; }
            return start;
        }

        private static DateTimeOffset? ReadTimestamp(string line, out int consumed) {
            consumed = 0;
            string text = line.TrimStart();
            int offset = line.Length - text.Length;
            if (text.StartsWith("[", StringComparison.Ordinal)) {
                int end = text.IndexOf(']');
                DateTimeOffset? timestamp = end > 1 ? ParseTimestamp(text.Substring(1, end - 1)) : null;
                if (timestamp.HasValue) { consumed = offset + end + 1; }
                return timestamp;
            }
            // Longest candidate first: keep a space-separated time/offset attached to its ISO date.
            int limit = Math.Min(text.Length, 40);
            for (int end = limit; end >= 10; end--) {
                if (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '[' && text[end] != '|') { continue; }
                DateTimeOffset? timestamp = ParseTimestamp(text.Substring(0, end));
                if (timestamp.HasValue) { consumed = offset + end; return timestamp; }
            }
            return null;
        }

        private static DateTimeOffset? ParseTimestamp(string candidate) {
            // Gate TryParse with an explicit complete ISO date, so it cannot fill in missing date components.
            if (candidate.Length < 10 || candidate[4] != '-' || candidate[7] != '-') { return null; }
            for (int i = 0; i < 10; i++) {
                if (i != 4 && i != 7 && (candidate[i] < '0' || candidate[i] > '9')) { return null; }
            }
            if (DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out DateTimeOffset timestamp)) {
                return timestamp;
            }
            return null;
        }
    }
}
