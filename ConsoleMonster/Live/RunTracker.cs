using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    /// <summary>One finished item of a run that failed or timed out, for the failures list.</summary>
    public sealed class RunFailure {
        internal RunFailure(int index, string label, string message, bool timedOut) {
            Index = index;
            Label = label;
            Message = message;
            TimedOut = timedOut;
        }

        /// <summary>The item's position in the input (0-based).</summary>
        public int Index { get; }

        /// <summary>The item's label.</summary>
        public string Label { get; }

        /// <summary>The error message.</summary>
        public string Message { get; }

        /// <summary>Whether the item ran out of time.</summary>
        public bool TimedOut { get; }
    }

    /// <summary>An item that is running, for the activity list.</summary>
    public sealed class RunActivity {
        internal RunActivity(int index, string label, TimeSpan started) {
            Index = index;
            Label = label;
            Started = started;
        }

        /// <summary>The item's position in the input (0-based).</summary>
        public int Index { get; }

        /// <summary>The item's label.</summary>
        public string Label { get; }

        /// <summary>When it started (run time).</summary>
        public TimeSpan Started { get; }
    }

    /// <summary>A consistent copy of a <see cref="RunTracker"/>.</summary>
    public sealed partial class RunSnapshot {
        internal RunSnapshot(IReadOnlyList<StatusKind> states, IReadOnlyList<RunActivity> running, IReadOnlyList<RunFailure> failures, TimeSpan elapsed, bool finished) {
            States = states;
            Running = running;
            Failures = failures;
            Elapsed = elapsed;
            IsFinished = finished;
            foreach (StatusKind state in states) {
                switch (state) {
                    case StatusKind.Pass:
                        Passed++;
                        break;
                    case StatusKind.Fail:
                        Failed++;
                        break;
                    case StatusKind.Warn:
                        Warned++;
                        break;
                    case StatusKind.Skip:
                        Skipped++;
                        break;
                }
            }
        }

        /// <summary>Each item's state, in input order.</summary>
        public IReadOnlyList<StatusKind> States { get; }

        /// <summary>The running items, oldest first.</summary>
        public IReadOnlyList<RunActivity> Running { get; }

        /// <summary>The failed and timed-out items, in the order they finished.</summary>
        public IReadOnlyList<RunFailure> Failures { get; }

        /// <summary>The run time (fixed once finished).</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>Whether the run finished.</summary>
        public bool IsFinished { get; }

        /// <summary>Items that passed.</summary>
        public int Passed { get; }

        /// <summary>Items that failed or timed out.</summary>
        public int Failed { get; }

        /// <summary>Items that finished with a warning.</summary>
        public int Warned { get; }

        /// <summary>Items skipped (not run because the run stopped).</summary>
        public int Skipped { get; }

        /// <summary>Items in total.</summary>
        public int Total => States.Count;

        /// <summary>Items finished, whatever the outcome.</summary>
        public int Done => Passed + Failed + Warned + Skipped;
    }

    /// <summary>
    /// The state of a run of many items (<see cref="ConsoleRun"/>, Invoke-ConsoleRun), safe to update from any thread: each
    /// item is pending, running or finished with an outcome; failures keep their message. <see cref="Widgets.RunView"/> draws it.
    /// </summary>
    public sealed partial class RunTracker {
        private readonly object _gate = new object();
        private readonly List<StatusKind> _states;
        private readonly Dictionary<int, RunActivity> _running = new Dictionary<int, RunActivity>();
        private readonly List<RunFailure> _failures = new List<RunFailure>();
        private readonly Func<TimeSpan> _clock;
        private TimeSpan? _finished;

        /// <summary>Creates a tracker.</summary>
        /// <param name="total">The number of items.</param>
        /// <param name="clock">The run clock, or null for a stopwatch started now.</param>
        public RunTracker(int total, Func<TimeSpan>? clock = null) {
            _states = new List<StatusKind>(new StatusKind[Math.Max(0, total)]);
            if (clock == null) {
                Stopwatch watch = Stopwatch.StartNew();
                clock = () => watch.Elapsed;
            }
            _clock = clock;
        }

        /// <summary>The number of items.</summary>
        public int Total { get { lock (_gate) { return _states.Count; } } }

        /// <summary>Adds a pending item to a count-based run whose work is discovered incrementally.</summary>
        /// <returns>The new item's zero-based index.</returns>
        /// <exception cref="InvalidOperationException">The run has already finished or has a fixed declaration of named items.</exception>
        public int Add() {
            lock (_gate) {
                if (_hasDeclaredItems) {
                    throw new InvalidOperationException("Cannot add undeclared items to a tracker with a fixed item declaration.");
                }
                if (_finished.HasValue) {
                    throw new InvalidOperationException("Cannot add items to a completed run.");
                }
                int index = _states.Count;
                _states.Add(StatusKind.Pending);
                return index;
            }
        }

        /// <summary>An item started.</summary>
        /// <param name="index">The item's position (0-based).</param>
        /// <param name="label">Its label.</param>
        public void Started(int index, string label) {
            lock (_gate) {
                if (_items.Length > 0) { StartItem(index); return; }
                if (index < 0 || index >= _states.Count) {
                    return;
                }
                StartedCore(index, label);
            }
        }

        private void StartedCore(int index, string label) {
            _states[index] = StatusKind.Running;
            _running[index] = new RunActivity(index, label ?? string.Empty, _clock());
        }

        /// <summary>An item finished.</summary>
        /// <param name="index">The item's position (0-based).</param>
        /// <param name="outcome">Pass, Fail, Warn or Skip.</param>
        /// <param name="label">Its label (for the failures list).</param>
        /// <param name="message">The error message of a failed item, or null.</param>
        /// <param name="timedOut">Whether it ran out of time.</param>
        public void Finished(int index, StatusKind outcome, string label, string? message = null, bool timedOut = false) {
            lock (_gate) {
                if (_items.Length > 0) { FinishItem(index, outcome, message, timedOut: timedOut); return; }
                if (index < 0 || index >= _states.Count) {
                    return;
                }
                FinishedCore(index, outcome, label, message, timedOut);
            }
        }

        private void FinishedCore(int index, StatusKind outcome, string label, string? message, bool timedOut) {
            _states[index] = outcome;
            _running.Remove(index);
            if (outcome == StatusKind.Fail) { _failures.Add(new RunFailure(index, label ?? string.Empty, message ?? string.Empty, timedOut)); }
        }

        /// <summary>The run ended: items never started count as skipped and the clock stops.</summary>
        public void Complete() {
            lock (_gate) {
                for (int i = 0; i < _states.Count; i++) {
                    if (_states[i] == StatusKind.Pending || _states[i] == StatusKind.Running) {
                        if (_items.Length > 0) { FinishItem(i, StatusKind.Skip); }
                        else { _states[i] = StatusKind.Skip; }
                    }
                }
                _running.Clear();
                _finished ??= _clock();
            }
        }

        /// <summary>A consistent copy of the state, for drawing on another thread.</summary>
        /// <returns>The snapshot.</returns>
        public RunSnapshot Snapshot() {
            lock (_gate) {
                List<RunActivity> running = new List<RunActivity>(_running.Values);
                running.Sort((a, b) => a.Started.CompareTo(b.Started));
                RunSnapshot snapshot = new RunSnapshot(_states.ToArray(), running, new List<RunFailure>(_failures), _finished ?? _clock(), _finished.HasValue);
                CopyItems(snapshot);
                return snapshot;
            }
        }
    }
}
