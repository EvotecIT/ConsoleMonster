using System;
using System.Collections.Generic;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Smoothed progress rate over a moving time window: the amount done since the start of the window divided by the time
    /// up to "now", so a stalled task's rate falls instead of freezing at its last value.
    /// </summary>
    internal sealed class RateMeter {
        private readonly Queue<KeyValuePair<TimeSpan, double>> _samples = new Queue<KeyValuePair<TimeSpan, double>>();
        private KeyValuePair<TimeSpan, double>? _baseline;
        private KeyValuePair<TimeSpan, double> _latest;

        internal TimeSpan Window { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Records the amount done at a time; the first record is the baseline (work started).</summary>
        internal void Record(TimeSpan time, double done) {
            KeyValuePair<TimeSpan, double> sample = new KeyValuePair<TimeSpan, double>(time, done);
            _latest = sample;
            if (_baseline == null) {
                _baseline = sample;
                return;
            }
            _samples.Enqueue(sample);
            Trim(time);
        }

        /// <summary>Items per second at <paramref name="now"/>, or 0 when unknown.</summary>
        internal double Rate(TimeSpan now) {
            if (_baseline == null) {
                return 0;
            }
            Trim(now);
            KeyValuePair<TimeSpan, double> start = _baseline.Value;
            double seconds = (now - start.Key).TotalSeconds;
            return seconds > 0.05 ? Math.Max(0, (_latest.Value - start.Value) / seconds) : 0;
        }

        // Samples older than the window become the baseline the rate is measured from.
        private void Trim(TimeSpan now) {
            TimeSpan from = now - Window;
            while (_samples.Count > 0 && _samples.Peek().Key < from) {
                _baseline = _samples.Dequeue();
            }
        }
    }
}
