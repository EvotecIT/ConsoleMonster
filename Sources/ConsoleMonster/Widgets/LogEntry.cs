using System;

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

        /// <summary>The text.</summary>
        public string Text { get; }
    }
}
