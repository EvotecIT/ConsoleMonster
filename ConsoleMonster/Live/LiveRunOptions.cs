using System;

namespace ConsoleMonster.Live {
    /// <summary>Display settings for a single-use <see cref="LiveRun"/>. Configure these before creating the run.</summary>
    public sealed class LiveRunOptions {
        /// <summary>The literal run name.</summary>
        public string Title { get; set; } = "Run";
        /// <summary>Force append-only output even on an interactive terminal.</summary>
        public bool NoLive { get; set; }
        /// <summary>Maximum share of terminal height used by the dashboard, default 0.4. Header and problems share the available rows on very short terminals.</summary>
        public double HeightFraction { get; set; } = 0.4;
        /// <summary>Show category progress when spare height is available.</summary>
        public bool ShowGroups { get; set; } = true;
        /// <summary>Enable p/f/g/? keyboard controls when input is available. Default false.</summary>
        public bool InteractiveKeys { get; set; }
        /// <summary>Show a short throughput trend next to the rate.</summary>
        public bool ShowTrend { get; set; }
        /// <summary>In append-only mode, emit progress after this many completions. Zero disables the count trigger.</summary>
        public int ProgressEvery { get; set; } = 25;
        /// <summary>In append-only mode, emit progress at this interval even while items are slow. Zero disables the time trigger.</summary>
        public TimeSpan ProgressInterval { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>Optional report location included in the final summary.</summary>
        public string? ReportPath { get; set; }
        /// <summary>Optional monotonic run clock for an embedding host or deterministic capture.</summary>
        public Func<TimeSpan>? Clock { get; set; }
        /// <summary>Exclusive configuration for this run's target, output, theme and capture. Do not share it with another session. The run sets inline mode, cooperative cancellation, ten frames per second, height limit and final summary.</summary>
        public LiveSessionOptions? Session { get; set; }
    }
}
