using System;

namespace ConsoleMonster.Live.Tails {
    /// <summary>A completed literal log line and the metadata recognised in that line.</summary>
    public sealed class TailedLogEntry {
        /// <summary>Creates a literal entry without interpreting markup or changing its text.</summary>
        /// <param name="text">The original line, excluding its line terminator.</param>
        /// <param name="level">A level or status word accepted by <see cref="Styling.LogLevels"/>; null means info.</param>
        /// <param name="timestamp">The timestamp written in the log, or null when none is known.</param>
        /// <param name="message">The display message after recognized metadata prefixes; null uses the original text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <param name="tag">The source (a provider, job or component name) shown in the tag column, or null.</param>
        public TailedLogEntry(string text, string? level = null, DateTimeOffset? timestamp = null, string? message = null, string? tag = null) {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Message = message ?? text;
            Styling.LogLevels.TryNormalize(level, out string normalized);
            Level = normalized;
            Timestamp = timestamp;
            Tag = string.IsNullOrWhiteSpace(tag) ? null : tag!.Trim();
        }

        /// <summary>The source the line came from, or null.</summary>
        public string? Tag { get; }

        /// <summary>The original literal text, including any timestamp, level markers and markup-like characters.</summary>
        public string Text { get; }

        /// <summary>The literal display message. Original text remains available in <see cref="Text"/> for filtering and plain output.</summary>
        public string Message { get; }

        /// <summary>The canonical log level: info, warn, err, verbose, debug, pass or skip.</summary>
        public string Level { get; }

        /// <summary>The log's timestamp, or null; missing timestamps are never replaced with the current time.</summary>
        public DateTimeOffset? Timestamp { get; }
    }
}
