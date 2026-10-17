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

        /// <summary>Session time of the entry.</summary>
        public TimeSpan Time { get; }

        /// <summary>The level.</summary>
        public string Level { get; }

        /// <summary>The text (the plain text of <see cref="RichText"/> when the entry is rich).</summary>
        public string Text { get; }

        /// <summary>A multicolour line, or null for literal <see cref="Text"/>.</summary>
        public RichText? RichText { get; }

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
