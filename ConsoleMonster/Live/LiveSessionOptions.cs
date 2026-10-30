using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    /// <summary>Where a live session draws.</summary>
    public enum LiveMode {
        /// <summary>The whole window, on the alternate screen; the final state is printed when it ends.</summary>
        FullScreen = 0,

        /// <summary>
        /// A live area as tall as its content, anchored below the cursor in the normal screen: output and log lines scroll
        /// above it, and the final state stays in the scrollback (or nothing, with PrintFinalState off).
        /// </summary>
        Inline = 1
    }

    /// <summary>Configuration for a single-use live session. Set before starting the session.</summary>
    public sealed class LiveSessionOptions {
        /// <summary>
        /// Full screen (default) or an inline area below the cursor. Without a VT console (CI, redirected output) both fall
        /// back to stream lines.
        /// </summary>
        public LiveMode Mode { get; set; }

        // A monitor can require append-only output after the host's scoped profile policy is applied.
        internal bool AppendOnly { get; set; }

        /// <summary>Allows live rendering when the terminal supports it; false selects append-only output.</summary>
        public bool Live { get => !AppendOnly; set => AppendOnly = !value; }

        private double _inlineHeightFraction = 1;
        /// <summary>Maximum fraction of the current terminal height occupied by inline output, between zero (exclusive) and one. Default one.</summary>
        public double InlineHeightFraction {
            get => _inlineHeightFraction;
            set {
                if (double.IsNaN(value) || value <= 0 || value > 1) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _inlineHeightFraction = value;
            }
        }

        private double _framesPerSecond = 15;
        private TimeSpan _stopTimeout = TimeSpan.FromSeconds(2);

        /// <summary>Maximum frame frequency, clamped to 1–60; defaults to 15.</summary>
        public double FramesPerSecond {
            get => _framesPerSecond;
            set {
                if (double.IsNaN(value) || double.IsInfinity(value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _framesPerSecond = Math.Max(1, Math.Min(60, value));
            }
        }

        /// <summary>Captures standard output and error while running; defaults to true.</summary>
        public bool CaptureConsole { get; set; } = true;

        /// <summary>Name of the log target, or null to discover the first log target.</summary>
        public string? LogTarget { get; set; }

        /// <summary>Profile override, or null for Terminal.Profile.</summary>
        public TerminalProfile? Profile { get; set; }

        /// <summary>
        /// The culture for numbers and dates in the session's widgets, overriding the profile's (and <c>CONSOLEMONSTER_CULTURE</c>);
        /// null keeps the profile culture, which defaults to the current culture.
        /// </summary>
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary>Theme override, or null for Terminal.Theme.</summary>
        public ITheme? Theme { get; set; }

        /// <summary>Borrowed output, or null to create and own the terminal output.</summary>
        public ITerminalOutput? Output { get; set; }

        /// <summary>
        /// Where the session draws: its profile, theme and writer (see <see cref="ITerminalTarget"/>), or null for
        /// <see cref="Terminal.Target"/> at start. <see cref="Profile"/>, <see cref="Theme"/> and <see cref="Output"/> still win
        /// when set. Only the console target claims the console, probes widths and watches the font.
        /// </summary>
        public ITerminalTarget? Target { get; set; }

        /// <summary>
        /// Whether the session drives the terminal's progress indicator (the Windows Terminal tab and taskbar) from its
        /// progress bars, lanes, spinners and status, and clears it at the end (default true; see <see cref="TaskbarProgress"/>).
        /// </summary>
        public bool TaskbarProgress { get; set; } = true;

        /// <summary>Whether the session rings the terminal bell when it ends, to call attention to a finished run (default false).</summary>
        public bool NotifyOnFinish { get; set; }

        /// <summary>UTF-8 recording destination, mutually exclusive with RecordWriter.</summary>
        public string? RecordPath { get; set; }

        /// <summary>Borrowed recording writer; flushed but left open at shutdown.</summary>
        public TextWriter? RecordWriter { get; set; }

        private int _autoRecord;
        private long _autoRecordMaxBytes = 50L * 1024 * 1024;

        /// <summary>
        /// Opt-in automatic recording: when above zero and neither <see cref="RecordPath"/> nor <see cref="RecordWriter"/> is
        /// set, the session records itself to <see cref="AutoRecordFolder"/> and keeps only this many recordings with its
        /// <see cref="RecordingName"/> (the newest ones; other files are never removed), so the last runs can be rewound with
        /// <c>Show-ConsoleReplay</c>. Default 0 (off).
        /// </summary>
        public int AutoRecord {
            get => _autoRecord;
            set => _autoRecord = Math.Max(0, value);
        }

        /// <summary>
        /// The most bytes the automatic recordings with one <see cref="RecordingName"/> may take together (default 50 MB): older
        /// recordings are removed first, and a single recording stops growing at this size.
        /// </summary>
        public long AutoRecordMaxBytes {
            get => _autoRecordMaxBytes;
            set {
                if (value < 1024) { throw new ArgumentOutOfRangeException(nameof(value), "AutoRecordMaxBytes must be at least 1024."); }
                _autoRecordMaxBytes = value;
            }
        }

        /// <summary>
        /// Where automatic recordings go, or null for <c>%LOCALAPPDATA%\ConsoleMonster\Recordings</c> (on Linux and macOS
        /// the local application data folder, usually <c>~/.local/share</c>).
        /// </summary>
        public string? AutoRecordFolder { get; set; }

        /// <summary>The name automatic recordings start with (default <c>session</c>), followed by the start time.</summary>
        public string? RecordingName { get; set; }

        /// <summary>The default folder for automatic recordings.</summary>
        public static string DefaultRecordingFolder => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConsoleMonster"), "Recordings");

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

        /// <summary>Stop drawing immediately on cooperative cancellation (default true). False leaves output open until Stop, allowing a runner to drain worker results before its final summary.</summary>
        public bool StopOnCancellation { get; set; } = true;

        /// <summary>
        /// Whether the session can be found through <see cref="LiveSession.Current"/> (default true), so update cmdlets and prompts
        /// started elsewhere reach it. A session that runs other code, such as a test run, turns it off: that code's own updates and
        /// prompts then behave as if no dashboard were running.
        /// </summary>
        public bool Discoverable { get; set; } = true;

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
