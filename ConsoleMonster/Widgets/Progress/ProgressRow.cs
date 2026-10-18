using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One row of a progress display as columns see it: a lane of <see cref="Lanes"/> or a <see cref="ProgressBar"/>, with
    /// its timing, smoothed rate and remaining time at the moment of rendering.
    /// </summary>
    public sealed class ProgressRow {
        internal ProgressRow(string key, string label, RichText? labelText, string? detail, RichText? detailText, StatusKind status,
            double done, double total, int passed, int failed, int warned, int skipped, TimeSpan? started, TimeSpan? finished,
            TimeSpan now, double rate, int order) {
            Key = key;
            Label = label;
            LabelText = labelText;
            Detail = detail;
            DetailText = detailText;
            Status = status;
            Done = done;
            Total = total;
            Passed = passed;
            Failed = failed;
            Warned = warned;
            Skipped = skipped;
            Started = started;
            Finished = finished;
            Now = now;
            Rate = rate;
            Order = order;
        }

        /// <summary>The row key (the lane key, or the progress widget's name).</summary>
        public string Key { get; }

        /// <summary>The label as plain text.</summary>
        public string Label { get; }

        /// <summary>The label as rich text, when it was given as rich text.</summary>
        public RichText? LabelText { get; }

        /// <summary>The detail text, or null.</summary>
        public string? Detail { get; }

        /// <summary>The detail as rich text, when it was given as rich text.</summary>
        public RichText? DetailText { get; }

        /// <summary>The status.</summary>
        public StatusKind Status { get; }

        /// <summary>The amount done.</summary>
        public double Done { get; }

        /// <summary>The total, or 0 when unknown.</summary>
        public double Total { get; }

        /// <summary>Steps that passed.</summary>
        public int Passed { get; }

        /// <summary>Steps that failed.</summary>
        public int Failed { get; }

        /// <summary>Steps with warnings.</summary>
        public int Warned { get; }

        /// <summary>Steps skipped.</summary>
        public int Skipped { get; }

        /// <summary>When the row started (session clock), or null.</summary>
        public TimeSpan? Started { get; }

        /// <summary>When the row finished (session clock), or null.</summary>
        public TimeSpan? Finished { get; }

        /// <summary>The session clock at render time.</summary>
        public TimeSpan Now { get; }

        /// <summary>The smoothed rate in items per second over the rate window, or 0 when unknown.</summary>
        public double Rate { get; }

        /// <summary>The display order.</summary>
        public int Order { get; }

        /// <summary>Whether the row has finished (pass, fail, warn or skip).</summary>
        public bool IsFinished => Status == StatusKind.Pass || Status == StatusKind.Fail || Status == StatusKind.Warn || Status == StatusKind.Skip;

        /// <summary>The fraction done, 0 to 1 (1 once finished).</summary>
        public double Fraction => IsFinished ? 1 : (Total <= 0 ? 0 : Math.Max(0, Math.Min(1, Done / Total)));

        /// <summary>The time spent so far (or in total, once finished), or null before the start.</summary>
        public TimeSpan? Elapsed {
            get {
                if (!Started.HasValue) {
                    return null;
                }
                TimeSpan end = Finished ?? Now;
                return end > Started.Value ? end - Started.Value : TimeSpan.Zero;
            }
        }

        /// <summary>The estimated time left from the smoothed rate, or null when it cannot be estimated.</summary>
        public TimeSpan? Remaining {
            get {
                if (IsFinished) {
                    return TimeSpan.Zero;
                }
                if (Total <= 0 || Rate <= 0 || Done >= Total) {
                    return null;
                }
                double seconds = (Total - Done) / Rate;
                return seconds < TimeSpan.MaxValue.TotalSeconds / 2 ? TimeSpan.FromSeconds(Math.Ceiling(seconds)) : (TimeSpan?)null;
            }
        }
    }
}
