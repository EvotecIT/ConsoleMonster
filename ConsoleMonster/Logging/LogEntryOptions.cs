using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;

namespace ConsoleMonster.Logging {
    // Framework-neutral entry options are internal to the core and its three logging adapters.
    internal sealed class LogEntryOptions {
        internal string? TimestampFormat { get; set; } = "HH:mm:ss";
        internal bool UseUtcTimestamp { get; set; }
        internal bool IncludeCategory { get; set; } = true;
        internal bool FullCategory { get; set; }
        internal int MaxExceptionLines { get; set; } = 12;
        internal string? LogRegion { get; set; } = "$log";
        internal TerminalProfile? Profile { get; set; }
        internal ITerminalWriter? Writer { get; set; }
    }
}
