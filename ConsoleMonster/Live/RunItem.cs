using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    /// <summary>A unit of work declared before a large run starts. Labels are literal text, not markup.</summary>
    public sealed class RunItem {
        /// <summary>Creates a named unit of work.</summary>
        /// <param name="label">Display name.</param>
        /// <param name="target">Optional machine, file or other target.</param>
        /// <param name="group">Optional category used by the group summary.</param>
        public RunItem(string label, string? target = null, string? group = null) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Target = target ?? string.Empty;
            Group = group ?? string.Empty;
        }
        /// <summary>The display name.</summary>
        public string Label { get; }
        /// <summary>The target, or an empty string.</summary>
        public string Target { get; }
        /// <summary>The group, or an empty string.</summary>
        public string Group { get; }
        /// <summary>The name followed by its optional target, for logs and activity rows.</summary>
        public string DisplayName => Label + (Target.Length > 0 ? " · " + Target : "");
    }

    /// <summary>An immutable observation of one declared item.</summary>
    public sealed class RunItemSnapshot {
        internal RunItemSnapshot(int index, RunItem item, StatusKind status, string label, string detail, TimeSpan duration, int completionOrder) {
            Index = index; Item = item; Status = status; Label = label; Detail = detail; Duration = duration;
            CompletionOrder = completionOrder;
        }
        /// <summary>The zero-based input position, also used for updates.</summary>
        public int Index { get; }
        /// <summary>The declared metadata.</summary>
        public RunItem Item { get; }
        /// <summary>The current state.</summary>
        public StatusKind Status { get; }
        /// <summary>The current phase label, for example collecting or evaluating (Update-Console -Run -Label).</summary>
        public string Label { get; }
        /// <summary>The completion detail, retained in full for the final summary (Update-Console -Run -Detail).</summary>
        public string Detail { get; }
        /// <summary>The elapsed running time or final duration.</summary>
        public TimeSpan Duration { get; }
        /// <summary>One-based completion order, or zero while pending or running.</summary>
        public int CompletionOrder { get; }
        /// <summary>Formats the literal fallback text for a completion event; the event state carries its outcome.</summary>
        /// <returns>Name, target, duration and a one-line message.</returns>
        public override string ToString() => Item.Label + (Item.Target.Length > 0 ? "   " + Item.Target : "") + "   " + NumberFormat.Duration(Duration)
            + (Detail.Length > 0 ? ": " + Detail.Replace("\r", " ").Replace("\n", " ") : "");
    }

    public sealed partial class RunSnapshot {
        /// <summary>Declared items and their state, or an empty list for a count-only tracker.</summary>
        public IReadOnlyList<RunItemSnapshot> Items { get; internal set; } = Array.Empty<RunItemSnapshot>();
        /// <summary>Completed items per second over the tracker's moving ten-second window.</summary>
        public double Rate { get; internal set; }
        /// <summary>Recent throughput samples, one per elapsed second, for a compact trend.</summary>
        public IReadOnlyList<double> Rates { get; internal set; } = Array.Empty<double>();
        /// <summary>Estimated remaining time from the moving rate, or null until the rate can be measured.</summary>
        public TimeSpan? Remaining => Rate > 0 && Total > Done ? TimeSpan.FromSeconds(Math.Min(TimeSpan.MaxValue.TotalSeconds / 2, (Total - Done) / Rate)) : (Done == Total ? TimeSpan.Zero : (TimeSpan?)null);
        /// <summary>Formats the same compact progress line used by CI output.</summary>
        /// <returns>Completion counts, ETA and throughput in items per minute.</returns>
        public string ProgressLine() => Done + "/" + Total + " · " + Passed + " passed · " + Failed + " failed · " + Warned + " warnings · " + Skipped + " skipped · ETA "
            + (Remaining.HasValue ? NumberFormat.Duration(Remaining.Value) : "--") + " · " + (Rate * 60).ToString("0.0", CultureInfo.InvariantCulture) + "/min";
    }
}
