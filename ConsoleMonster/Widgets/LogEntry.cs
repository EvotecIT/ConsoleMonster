using System;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>One line in a <see cref="LogRegion"/>.</summary>
    public sealed class LogEntry {
        /// <summary>Creates an entry.</summary>
        /// <param name="time">Session time of the entry.</param>
        /// <param name="level">Level: out, err, info, warn, verbose, debug, host, pass, fail.</param>
        /// <param name="text">The text (single line).</param>
        public LogEntry(TimeSpan time, string level, string text) {
            Time = time;
            Level = level;
            Text = text;
        }

        /// <summary>Creates a literal entry with session time and an optional source timestamp.</summary>
        /// <param name="time">Session time of the entry.</param>
        /// <param name="timestamp">The source timestamp, or null when unknown.</param>
        /// <param name="level">The canonical log level.</param>
        /// <param name="text">The original literal text.</param>
        public LogEntry(TimeSpan time, string level, string text, DateTimeOffset? timestamp) : this(time, level, text) {
            Timestamp = timestamp;
        }

        /// <summary>The source timestamp, or null when none was supplied.</summary>
        public DateTimeOffset? Timestamp { get; }

        /// <summary>Session time of the entry.</summary>
        public TimeSpan Time { get; }

        /// <summary>The level.</summary>
        public string Level { get; }

        /// <summary>The logical line's plain text, without markup.</summary>
        public string Text { get; }

        /// <summary>The rich source, or null for literal <see cref="Text"/>. Select <see cref="LineIndex"/> after rendering the source.</summary>
        public RichText? RichText { get; }

        /// <summary>Zero-based line in <see cref="RichText"/> represented by this entry; zero for single-line or literal entries.</summary>
        public int LineIndex { get; }

        internal bool IsLineSlice { get; }

        internal LogEntry(TimeSpan time, string level, RichText source, string text, int lineIndex) {
            Time = time;
            Level = level;
            RichText = source;
            Text = text;
            LineIndex = lineIndex;
            IsLineSlice = true;
        }

        /// <summary>Creates a multicolour entry.</summary>
        /// <param name="time">Session time of the entry.</param>
        /// <param name="level">The level.</param>
        /// <param name="text">The rich line.</param>
        public LogEntry(TimeSpan time, string level, RichText text) {
            Time = time;
            Level = level;
            RichText = text ?? throw new ArgumentNullException(nameof(text));
            Text = text.PlainText;
        }
    }
}
