using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    /// <summary>Configuration for a single-use live session. Set before starting the session.</summary>
    public sealed class LiveSessionOptions {
        private int _framesPerSecond = 15;
        private TimeSpan _stopTimeout = TimeSpan.FromSeconds(2);

        /// <summary>Maximum frame frequency, clamped to 1–60; defaults to 15.</summary>
        public int FramesPerSecond {
            get => _framesPerSecond;
            set => _framesPerSecond = Math.Max(1, Math.Min(60, value));
        }

        /// <summary>Captures standard output and error while running; defaults to true.</summary>
        public bool CaptureConsole { get; set; } = true;

        /// <summary>Name of the log target, or null to discover the first log target.</summary>
        public string? LogTarget { get; set; }

        /// <summary>Profile override, or null for Terminal.Profile.</summary>
        public TerminalProfile? Profile { get; set; }

        /// <summary>Theme override, or null for Terminal.Theme.</summary>
        public ITheme? Theme { get; set; }

        /// <summary>Borrowed output, or null to create and own the terminal output.</summary>
        public ITerminalOutput? Output { get; set; }

        /// <summary>UTF-8 recording destination, mutually exclusive with RecordWriter.</summary>
        public string? RecordPath { get; set; }

        /// <summary>Borrowed recording writer; flushed but left open at shutdown.</summary>
        public TextWriter? RecordWriter { get; set; }

        /// <summary>Widget appended to scrollback after live output ends.</summary>
        public IWidget? Summary { get; set; }

        /// <summary>
        /// When no <see cref="Summary"/> is set, print the final state of the root widget to scrollback after the session ends
        /// (default true), so a live dashboard always leaves a record behind.
        /// </summary>
        public bool PrintFinalState { get; set; } = true;

        /// <summary>Interval between line-mode summaries; defaults to ten seconds. Zero disables them.</summary>
        public TimeSpan StreamHeartbeat { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Prefixes each line-mode narration and heartbeat line with the session clock (<c>[mm:ss]</c>, or <c>[h:mm:ss]</c>
        /// past an hour). Null (the default) enables it when the profile detected a CI environment, where log viewers often
        /// lack their own per-line timestamps; true or false forces it. Interactive frames are unaffected.
        /// </summary>
        public bool? StreamTimestamps { get; set; }

        /// <summary>Intercepts the first Ctrl+C for cooperative cancellation; defaults to true.</summary>
        public bool HandleCancelKey { get; set; } = true;

        /// <summary>Maximum render-thread join wait; defaults to two seconds. Must be between zero and Int32.MaxValue milliseconds.</summary>
        public TimeSpan StopTimeout {
            get => _stopTimeout;
            set {
                if (value < TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _stopTimeout = value;
            }
        }

        /// <summary>Optional monotonic time source, sampled relative to its value at Start.</summary>
        public Func<TimeSpan>? Clock { get; set; }

        /// <summary>
        /// Test hook: returns the window as { left, top, width, height } (or null for unknown) instead of reading the console.
        /// </summary>
        internal Func<int[]?>? WindowSource { get; set; }

        /// <summary>Optional replay delay implementation; receives the scaled interval and cancellation token.</summary>
        public Action<TimeSpan, CancellationToken>? ReplayDelay { get; set; }
    }
}
