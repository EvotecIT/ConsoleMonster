using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleMonster.Sessions {
    /// <summary>Local terminal recording settings. Input bytes are never recorded.</summary>
    public sealed class SessionRecordingOptions {
        internal bool CaptureInitiallyEnabled { get; set; } = true;
        /// <summary>Native asciicast v2 output. Null creates a timestamped name in <see cref="Folder"/>.</summary>
        public string? Path { get; set; }
        /// <summary>Folder used for automatic recording names. Resolving the default does not probe its availability.</summary>
        public string Folder { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments, System.Environment.SpecialFolderOption.DoNotVerify), "ConsoleMonster", "Recordings");
        /// <summary>Human-readable recording title.</summary>
        public string Title { get; set; } = "Console session";
        /// <summary>Child executable. Null selects $SHELL on Unix, falling back to zsh on macOS or sh on Linux; Windows uses its platform shell.</summary>
        public string? Shell { get; set; }
        /// <summary>Individual child arguments, quoted by the transport.</summary>
        public IList<string> Arguments { get; } = new List<string>();
        /// <summary>Named child environment overrides. Null removes a variable; values are never written to metadata.</summary>
        public IDictionary<string, string?> Environment { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);
        /// <summary>Initial terminal width.</summary>
        public int Width { get; set; }
        /// <summary>Initial terminal height.</summary>
        public int Height { get; set; } = 30;
        /// <summary>Maximum cast bytes, including its header. Capture stops at the limit; the child keeps running.</summary>
        public long MaxBytes { get; set; } = 64 * 1024 * 1024;
        /// <summary>Asciicast player idle-limit hint. Source timestamps are always preserved; zero disables the hint.</summary>
        public TimeSpan IdleLimit { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>Maximum time spent waiting for the child in WaitForExit.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(24);
        /// <summary>Optional maximum child lifetime from startup, including readiness and scripted-input waits.
        /// Null preserves the independent wait timeout used by existing callers.</summary>
        public TimeSpan? SessionTimeout { get; set; }
        /// <summary>Additional regular expressions, applied before any recording data is written.</summary>
        public IList<string> Redact { get; } = new List<string>();
        /// <summary>Visible replacement for matched output. The default is [REDACTED]; control characters are not allowed.</summary>
        public string RedactionText { get; set; } = "[REDACTED]";
        /// <summary>Child working directory.</summary>
        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;
        /// <summary>Explicitly capture hosted output through <see cref="SessionRecorder.CaptureOutput"/> without starting a
        /// child terminal. The host owns output forwarding; input and arbitrary Console writes are not intercepted.</summary>
        public bool InProcess { get; set; }
        /// <summary>Answers cursor-position queries from the tracked VT screen for unattended child sessions.
        /// Leave false when forwarding output to a real terminal that supplies its own replies. ConPTY already
        /// answers native Windows console queries; the emulation is used only on Unix.</summary>
        public bool RespondToCursorQueries { get; set; }

        internal void Validate() {
            Width = Terminal.ResolveWidth(Width);
            if (Width < 2 || Width > 1000 || Height < 2 || Height > 1000 || (long)Width * Height > 250000) {
                throw new ArgumentOutOfRangeException(nameof(Width), "Use 2–1000 cells per dimension and at most 250000 cells.");
            }
            if (MaxBytes < 1024) { throw new ArgumentOutOfRangeException(nameof(MaxBytes)); }
            if (IdleLimit < TimeSpan.Zero || Timeout <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(Timeout)); }
            if (SessionTimeout <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(SessionTimeout)); }
            if (string.IsNullOrEmpty(RedactionText) || RedactionText.Length > 128) { throw new ArgumentException("RedactionText must contain 1 to 128 visible characters.", nameof(RedactionText)); }
            foreach (char value in RedactionText) { if (char.IsControl(value)) { throw new ArgumentException("RedactionText cannot contain control characters.", nameof(RedactionText)); } }
            if (!Directory.Exists(WorkingDirectory)) { throw new DirectoryNotFoundException(WorkingDirectory); }
            if (Shell != null && Shell.IndexOf('\0') >= 0) { throw new ArgumentException("Shell contains a NUL character."); }
            foreach (string argument in Arguments) { if (argument == null || argument.IndexOf('\0') >= 0) { throw new ArgumentException("Child arguments cannot contain NUL characters."); } }
            foreach (KeyValuePair<string, string?> variable in Environment) {
                if (variable.Key.Length == 0 || variable.Key.IndexOfAny(new[] { '=', '\0' }) >= 0 || (variable.Value != null && variable.Value.IndexOf('\0') >= 0)) { throw new ArgumentException("Invalid child environment override."); }
            }
        }
    }
}
