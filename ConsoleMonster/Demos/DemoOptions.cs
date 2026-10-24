using System;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.Demos {
    /// <summary>Settings for scripted or stage-driven PowerShell demos.</summary>
    public sealed class DemoOptions {
        /// <summary>PTY, output path, dimensions, environment, redaction and idle limit. Shell defaults to pwsh.</summary>
        public SessionRecordingOptions Recording { get; set; } = new SessionRecordingOptions { Shell = "pwsh" };
        /// <summary>Seed for repeatable typing jitter.</summary>
        public int Seed { get; set; } = 42;
        /// <summary>Fractional variation around each character delay, from zero to one.</summary>
        public double Jitter { get; set; } = 0.2;
        /// <summary>Maximum time to wait for a semantic prompt or output match.</summary>
        public TimeSpan StepTimeout { get; set; } = TimeSpan.FromSeconds(30);
        /// <summary>Live output callback. Called on the PTY reader thread; use a thread-safe relay from PowerShell.</summary>
        public Action<string>? Output { get; set; }
    }
    /// <summary>Seeded character delays, independent of operating system and System.Random implementation.</summary>
    public sealed class DemoTypingClock {
        private uint _state;
        /// <summary>Creates a repeatable typing schedule.</summary>
        public DemoTypingClock(int seed) { _state = unchecked((uint)seed); if (_state == 0) { _state = 0x6d2b79f5; } }
        /// <summary>Gets the next delay using five characters per word and bounded jitter.</summary>
        public TimeSpan Next(double wordsPerMinute, double jitter = 0.2) {
            if (double.IsNaN(wordsPerMinute) || double.IsInfinity(wordsPerMinute) || wordsPerMinute < 1 || wordsPerMinute > 1000) { throw new ArgumentOutOfRangeException(nameof(wordsPerMinute)); }
            if (double.IsNaN(jitter) || jitter < 0 || jitter > 1) { throw new ArgumentOutOfRangeException(nameof(jitter)); }
            _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
            double variation = (_state / (double)uint.MaxValue * 2 - 1) * jitter;
            return TimeSpan.FromSeconds(12 / wordsPerMinute * (1 + variation));
        }
    }
}
