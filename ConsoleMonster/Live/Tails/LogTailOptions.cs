using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live.Tails {
    /// <summary>Configuration for a synchronous, polling log-tail run.</summary>
    public sealed class LogTailOptions {
        /// <summary>Maximum retained history lines (default 1000; minimum 1). Pending source updates are lossless.</summary>
        public int Capacity { get; set; } = 1000;

        /// <summary>Source-read iterations; one processes one complete snapshot, zero follows until cancelled or quit.</summary>
        public long Count { get; set; }

        /// <summary>Literal, ordinal case-insensitive substring required in the original line; null or empty accepts all.</summary>
        public string? Filter { get; set; }

        /// <summary>The lowest level kept (debug, verbose, info, warn or err), or null for every level; warn keeps warnings and errors.</summary>
        public string? Level { get; set; }

        /// <summary>Literal, ordinal case-insensitive substring emphasized in live frames; plain lines stay unchanged.</summary>
        public string? Highlight { get; set; }

        /// <summary>Invariant source timestamp format, or null for HH:mm:ss, adding yyyy-MM-dd when retained source dates differ.
        /// Applies only to the live view, at every terminal width; plain original lines and source metadata are unchanged.</summary>
        public string? TimestampFormat { get; set; }

        /// <summary>Time between source reads (default 250 milliseconds; positive and at most Int32.MaxValue milliseconds).</summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>Enables the live viewer when the host supports input; false writes literal lines without acquiring input.</summary>
        public bool Live { get; set; } = true;

        /// <summary>Receives each original line in plain mode, without a terminator. Null uses the core terminal writer.</summary>
        public Action<string>? WriteLine { get; set; }

        /// <summary>Live layout, full screen by default. Plain fallback ignores this setting.</summary>
        public LiveMode Mode { get; set; } = LiveMode.FullScreen;

        /// <summary>Terminal profile override, or null for the current terminal target's profile.</summary>
        public TerminalProfile? Profile { get; set; }

        /// <summary>Theme override, or null for the current terminal target's theme.</summary>
        public ITheme? Theme { get; set; }

        /// <summary>Borrowed live output, or null for the core terminal output. A non-live output selects plain fallback.</summary>
        public ITerminalOutput? Output { get; set; }

        /// <summary>Borrowed input source leased through ConsoleInput, or null for the current target, input override or console.</summary>
        public IInputSource? Input { get; set; }

        /// <summary>Whether to handle the console's Ctrl+C signal cooperatively (default true).</summary>
        public bool HandleCancelKey { get; set; } = true;

        internal void Validate() {
            if (TimestampFormat != null) { DateTimeOffset.MinValue.ToString(TimestampFormat, System.Globalization.CultureInfo.InvariantCulture); }
            _ = new Widgets.LogFilter(level: Level);
            if (Capacity < 1) { throw new ArgumentOutOfRangeException(nameof(Capacity)); }
            if (Count < 0) { throw new ArgumentOutOfRangeException(nameof(Count)); }
            if (PollInterval <= TimeSpan.Zero || PollInterval.TotalMilliseconds > int.MaxValue) {
                throw new ArgumentOutOfRangeException(nameof(PollInterval));
            }
            if (Mode != LiveMode.FullScreen && Mode != LiveMode.Inline) { throw new ArgumentOutOfRangeException(nameof(Mode)); }
        }
    }
}
