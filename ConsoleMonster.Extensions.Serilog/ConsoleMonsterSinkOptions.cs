using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;

namespace ConsoleMonster.Extensions.Serilog {
    /// <summary>Controls ConsoleMonster's Serilog line rendering and output destination.</summary>
    public sealed class ConsoleMonsterSinkOptions {
        /// <summary>The invariant timestamp format, or null to omit timestamps (default HH:mm:ss).</summary>
        public string? TimestampFormat { get; set; } = "HH:mm:ss";
        /// <summary>Whether event timestamps are converted to UTC.</summary>
        public bool UseUtcTimestamp { get; set; }
        /// <summary>Whether the last part of SourceContext is shown (default true).</summary>
        public bool IncludeCategory { get; set; } = true;
        /// <summary>The maximum exception frames; zero shows only exception types and messages (default 12).</summary>
        public int MaxExceptionLines { get; set; } = 12;
        /// <summary>The live log region key (default $log), or null to always print above the view.</summary>
        public string? LogRegion { get; set; } = "$log";
        /// <summary>The terminal profile, or null for automatic detection.</summary>
        public TerminalProfile? Profile { get; set; }
        /// <summary>An explicit output writer, or null for the terminal. The caller retains ownership.</summary>
        public ITerminalWriter? Writer { get; set; }
    }
}
