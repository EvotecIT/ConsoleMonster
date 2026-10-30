using System;
using System.Diagnostics;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {

    /// <summary>Polls the current Windows console font without creating a thread or timer.</summary>
    internal sealed class ConsoleFontWatcher {
        private readonly TimeSpan _pollingInterval;
        private readonly Func<TimeSpan> _clock;
        private readonly FontReader _reader;
        private TimeSpan _lastPoll;
        private bool _hasPolled;
        private bool _hasFont;
        private string? _face;
        private int _height;

        /// <summary>Creates a watcher with a one-second polling interval.</summary>
        public ConsoleFontWatcher() : this(TimeSpan.FromSeconds(1)) { }

        /// <summary>Creates a watcher with the specified polling interval.</summary>
        /// <param name="pollingInterval">Minimum time between native font reads; nonpositive values use one second.</param>
        public ConsoleFontWatcher(TimeSpan pollingInterval) : this(pollingInterval, ReadCurrentFont,
            () => TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency)) { }

        internal ConsoleFontWatcher(TimeSpan pollingInterval, FontReader reader, Func<TimeSpan> clock) {
            _pollingInterval = pollingInterval > TimeSpan.Zero ? pollingInterval : TimeSpan.FromSeconds(1);
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Reads the font at most once per interval and reports a changed face or height.</summary>
        /// <param name="face">The current face when a font reading succeeds; otherwise null.</param>
        /// <param name="height">The current font height when a font reading succeeds; otherwise zero.</param>
        /// <returns>True when a successful reading differs from the previous successful reading.</returns>
        public bool CheckChanged(out string? face, out int height) {
            face = null;
            height = 0;
            TimeSpan now = _clock();
            if (_hasPolled && now - _lastPoll < _pollingInterval) {
                return false;
            }
            _hasPolled = true;
            _lastPoll = now;
            if (!_reader(out string? currentFace, out int currentHeight)) {
                return false;
            }
            face = currentFace;
            height = currentHeight;
            bool changed = _hasFont && (!string.Equals(_face, currentFace, StringComparison.Ordinal) || _height != currentHeight);
            _hasFont = true;
            _face = currentFace;
            _height = currentHeight;
            return changed;
        }

        private static bool ReadCurrentFont(out string? face, out int height) {
            face = null;
            height = 0;
            return WindowsConsole.IsWindows && WindowsConsole.TryGetFont(WindowsConsole.StandardOutput, out face, out _, out height);
        }

        internal delegate bool FontReader(out string? face, out int height);
    }
}
