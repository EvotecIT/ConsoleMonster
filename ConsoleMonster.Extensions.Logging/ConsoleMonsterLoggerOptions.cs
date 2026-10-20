using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using Microsoft.Extensions.Logging;

namespace ConsoleMonster.Extensions.Logging {
    /// <summary>How a category is shown in a log line.</summary>
    public enum LogCategoryFormat {
        /// <summary>The last part of the category (<c>Orders</c> for <c>Shop.Services.Orders</c>), the default.</summary>
        Short,

        /// <summary>The full category.</summary>
        Full,

        /// <summary>No category.</summary>
        None
    }

    /// <summary>Options for <see cref="ConsoleMonsterLoggerProvider"/>.</summary>
    public sealed class ConsoleMonsterLoggerOptions {
        /// <summary>The lowest level written (default <see cref="LogLevel.Trace"/>; the logging builder's filters apply first).</summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        /// <summary>The timestamp format (a .NET date format, default <c>HH:mm:ss</c>), or null for no timestamp.</summary>
        public string? TimestampFormat { get; set; } = "HH:mm:ss";

        /// <summary>Whether timestamps are UTC (default false: local time).</summary>
        public bool UseUtcTimestamp { get; set; }

        /// <summary>How the category is shown (default <see cref="LogCategoryFormat.Short"/>).</summary>
        public LogCategoryFormat Category { get; set; } = LogCategoryFormat.Short;

        /// <summary>Whether scopes are shown after the category (default true).</summary>
        public bool IncludeScopes { get; set; } = true;

        /// <summary>Whether the event id is shown (default false).</summary>
        public bool IncludeEventId { get; set; }

        /// <summary>The most lines of an exception's details printed under its log line (default 12; 0 for the type and message only).</summary>
        public int MaxExceptionLines { get; set; } = 12;

        /// <summary>
        /// The log region of a running live session that receives the lines (default <c>$log</c>, the dashboard's log); when
        /// no session with that region runs, lines are printed. Null always prints.
        /// </summary>
        public string? LogRegion { get; set; } = "$log";

        /// <summary>The terminal profile to write for, or null for the current terminal (colour, glyphs and redirection detected).</summary>
        public TerminalProfile? Profile { get; set; }

        /// <summary>Where printed lines go, or null for the current terminal.</summary>
        public ITerminalWriter? Writer { get; set; }
    }
}
