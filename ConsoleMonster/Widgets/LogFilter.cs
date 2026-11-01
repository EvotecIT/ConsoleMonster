using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Which log entries a log region, log viewport or log tail shows: one source tag, a minimum level and a literal
    /// text. Every part that is set must match. Hidden entries stay in the retained history, so changing the filter
    /// brings them back.
    /// </summary>
    public sealed class LogFilter {
        /// <summary>Creates a filter; null or empty parts match every entry.</summary>
        /// <param name="tag">The source tag to show, compared without case, or null for every source.</param>
        /// <param name="level">The lowest level to show (debug, verbose, info, warn, err; pass and skip rank with info), or null for every level.</param>
        /// <param name="text">Literal text the entry must contain, compared without case, or null.</param>
        /// <exception cref="ArgumentException">The level is not a log level or status word.</exception>
        public LogFilter(string? tag = null, string? level = null, string? text = null) {
            Tag = string.IsNullOrWhiteSpace(tag) ? null : tag!.Trim();
            if (!string.IsNullOrWhiteSpace(level)) {
                if (!LogLevels.TryNormalize(level, out string normalized)) {
                    throw new ArgumentException("'" + level + "' is not a log level. Use debug, verbose, info, warn or err.", nameof(level));
                }
                Level = normalized;
            }
            Text = string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>The source tag to show, or null.</summary>
        public string? Tag { get; }

        /// <summary>The lowest level shown, as a canonical level name, or null.</summary>
        public string? Level { get; }

        /// <summary>Literal text an entry must contain, or null.</summary>
        public string? Text { get; }

        /// <summary>Whether every part is empty, so the filter shows everything.</summary>
        public bool IsEmpty => Tag == null && Level == null && Text == null;

        /// <summary>Whether an entry passes the filter.</summary>
        /// <param name="entry">The entry.</param>
        /// <returns>True when the entry is shown.</returns>
        public bool Matches(LogEntry entry) {
            if (entry == null) { throw new ArgumentNullException(nameof(entry)); }
            return Matches(entry.Text, entry.Level, entry.Tag);
        }

        internal bool Matches(string text, string level, string? tag) {
            if (Tag != null && !string.Equals(Tag, tag, StringComparison.OrdinalIgnoreCase)) { return false; }
            if (Level != null && Rank(LogTags.Normalize(level)) < Rank(Level)) { return false; }
            return Text == null || (text ?? string.Empty).IndexOf(Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Severity order for the minimum-level filter; pass and skip are outcomes reported at ordinary volume.
        private static int Rank(string level) {
            switch (level) {
                case "debug": return 0;
                case "verbose": return 1;
                case "warn": return 3;
                case "err": return 4;
                default: return 2;
            }
        }
    }
}
