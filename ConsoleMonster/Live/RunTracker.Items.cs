using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class RunTracker {
        private RunItem[] _items = Array.Empty<RunItem>();
        private readonly bool _hasDeclaredItems;
        private string[] _phases = Array.Empty<string>();
        private string[] _messages = Array.Empty<string>();
        private TimeSpan[] _durations = Array.Empty<TimeSpan>();
        private readonly RateMeter _rate = new RateMeter();
        private readonly Queue<double> _rates = new Queue<double>();
        private int _doneItems;
        private int[] _completionOrder = Array.Empty<int>();
        private long _sampleSecond = -1;

        /// <summary>Creates a tracker with a fixed declaration of names, targets and categories for queued work.</summary>
        /// <param name="items">Items in input order; their position is the update key.</param>
        /// <param name="clock">Optional monotonic clock, used by deterministic captures and embedded hosts.</param>
        public RunTracker(IEnumerable<RunItem> items, Func<TimeSpan>? clock = null) : this(Materialize(items), clock) { }

        private RunTracker(RunItem[] items, Func<TimeSpan>? clock) : this(items.Length, clock) {
            _hasDeclaredItems = true;
            _items = items;
            _phases = new string[items.Length]; _messages = new string[items.Length]; _durations = new TimeSpan[items.Length];
            _completionOrder = new int[items.Length];
            _rate.Record(_clock(), 0);
        }

        private static RunItem[] Materialize(IEnumerable<RunItem> items) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            RunItem[] result = items.ToArray();
            if (result.Any(item => item == null)) { throw new ArgumentException("Items cannot contain null.", nameof(items)); }
            return result;
        }

        /// <summary>Starts a declared item or updates its phase. Finished items cannot restart.</summary>
        /// <param name="index">Zero-based input position.</param>
        /// <param name="phase">Literal phase text.</param>
        /// <returns>False for an already finished item or completed run.</returns>
        public bool StartItem(int index, string? phase = null) {
            lock (_gate) {
                CheckItem(index);
                if (_finished.HasValue || IsTerminal(_states[index])) { return false; }
                if (_states[index] != StatusKind.Running) { StartedCore(index, _items[index].Name); }
                _phases[index] = phase ?? string.Empty;
                return true;
            }
        }

        /// <summary>Completes a declared item exactly once, retaining its message and duration.</summary>
        /// <param name="index">Zero-based input position.</param>
        /// <param name="outcome">Pass, Fail, Warn or Skip.</param>
        /// <param name="message">Full literal completion message.</param>
        /// <param name="duration">Explicit duration, or null to use the running clock.</param>
        /// <param name="timedOut">Whether the item's failure was a timeout.</param>
        /// <returns>The new completion, or null if it was already completed.</returns>
        public RunItemSnapshot? FinishItem(int index, StatusKind outcome, string? message = null, TimeSpan? duration = null, bool timedOut = false) {
            if (!IsTerminal(outcome)) { throw new ArgumentOutOfRangeException(nameof(outcome), "Use Pass, Fail, Warn or Skip."); }
            if (duration < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(duration)); }
            lock (_gate) {
                CheckItem(index);
                if (_finished.HasValue || IsTerminal(_states[index])) { return null; }
                TimeSpan now = _clock();
                _durations[index] = duration ?? (_running.TryGetValue(index, out RunActivity? active) ? now - active.Started : TimeSpan.Zero);
                _messages[index] = message ?? string.Empty;
                FinishedCore(index, outcome, _items[index].Name, message, timedOut);
                _doneItems++;
                _completionOrder[index] = _doneItems;
                _rate.Record(now, _doneItems);
                return ItemAt(index, now);
            }
        }

        private void CheckItem(int index) {
            if (index < 0 || index >= _items.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
        }
        internal static bool IsTerminal(StatusKind state) => state == StatusKind.Pass || state == StatusKind.Fail || state == StatusKind.Warn || state == StatusKind.Skip;

        private RunItemSnapshot ItemAt(int index, TimeSpan now) {
            TimeSpan duration = _running.TryGetValue(index, out RunActivity? active) ? now - active.Started : _durations[index];
            return new RunItemSnapshot(index, _items[index], _states[index], _phases[index] ?? string.Empty, _messages[index] ?? string.Empty, duration, _completionOrder[index]);
        }

        private void CopyItems(RunSnapshot snapshot) {
            if (_items.Length == 0) { return; }
            RunItemSnapshot[] items = new RunItemSnapshot[_items.Length];
            for (int i = 0; i < items.Length; i++) { items[i] = ItemAt(i, snapshot.Elapsed); }
            snapshot.Items = items;
            snapshot.Rate = _rate.Rate(snapshot.Elapsed);
            long second = (long)snapshot.Elapsed.TotalSeconds;
            if (second != _sampleSecond && !_finished.HasValue) {
                _sampleSecond = second; _rates.Enqueue(snapshot.Rate * 60);
                while (_rates.Count > 24) { _rates.Dequeue(); }
            }
            snapshot.Rates = _rates.ToArray();
        }
    }
}
