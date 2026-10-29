using System;
using System.Diagnostics;

namespace ConsoleMonster.Values {
    /// <summary>
    /// The time and node allowance of one shaping pass. Getters that reach a remote system (directory entries, COM objects,
    /// lazy collections) can take seconds each; once the allowance is spent the remaining values are not read and show as
    /// "…" instead of stalling the output.
    /// </summary>
    public sealed class ShapeBudget {
        private readonly Func<TimeSpan> _elapsed;
        private readonly TimeSpan _time;
        private int _nodes;

        /// <summary>Creates a budget.</summary>
        /// <param name="time">How long the pass may take.</param>
        /// <param name="maxNodes">How many values it may read.</param>
        public ShapeBudget(TimeSpan time, int maxNodes) : this(time, maxNodes, StartClock()) { }

        // One elapsed clock per pass; the public constructor always uses a running monotonic Stopwatch.
        internal ShapeBudget(TimeSpan time, int maxNodes, Func<TimeSpan> elapsed) {
            _elapsed = elapsed ?? throw new ArgumentNullException(nameof(elapsed));
            _time = time;
            MaxNodes = Math.Max(1, maxNodes);
        }

        private static Func<TimeSpan> StartClock() {
            Stopwatch clock = Stopwatch.StartNew();
            return () => clock.Elapsed;
        }

        /// <summary>A budget from options.</summary>
        /// <param name="options">The options, or null for the defaults.</param>
        /// <returns>The budget.</returns>
        public static ShapeBudget For(ShapeOptions? options) {
            ShapeOptions o = options ?? new ShapeOptions();
            return new ShapeBudget(o.TimeBudget, o.MaxNodes);
        }

        /// <summary>The most values the pass may read.</summary>
        public int MaxNodes { get; }

        /// <summary>Whether the allowance is spent (time or nodes).</summary>
        public bool Exhausted => _nodes >= MaxNodes || TimeExceeded;

        /// <summary>Whether the elapsed time allowance has expired.</summary>
        internal bool TimeExceeded => _time > TimeSpan.Zero && _elapsed() > _time;

        /// <summary>Whether the pass stopped reading at some point.</summary>
        public bool WasExceeded { get; private set; }

        private static readonly TimeSpan StallThreshold = TimeSpan.FromMilliseconds(200);
        private readonly System.Collections.Generic.HashSet<object> _slow = new System.Collections.Generic.HashSet<object>();
        private readonly System.Collections.Generic.HashSet<object> _fast = new System.Collections.Generic.HashSet<object>();
        private TimeSpan _stalled;

        /// <summary>
        /// Runs one read for a source (a column, a field), timing it. Reads slower than 200 ms count as stalls; once the stalls
        /// add up to the time allowance, only sources that have already read quickly are still read (the others give
        /// <see cref="BudgetExceeded"/>), so a large but healthy table is never cut short and remote getters cannot hold it up.
        /// </summary>
        /// <param name="source">What is read (a column or field identity).</param>
        /// <param name="read">The read.</param>
        /// <returns>The value, or <see cref="BudgetExceeded.Instance"/>.</returns>
        public object? Guard(object source, Func<object?> read) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
            if (read == null) {
                throw new ArgumentNullException(nameof(read));
            }
            lock (_slow) {
                if (_time > TimeSpan.Zero && _stalled > _time && (_slow.Contains(source) || !_fast.Contains(source))) {
                    WasExceeded = true;
                    return BudgetExceeded.Instance;
                }
            }
            Stopwatch clock = Stopwatch.StartNew();
            object? value = read();
            clock.Stop();
            lock (_slow) {
                if (clock.Elapsed > StallThreshold) {
                    _stalled += clock.Elapsed;
                    _slow.Add(source);
                    _fast.Remove(source);
                } else if (!_slow.Contains(source)) {
                    _fast.Add(source);
                }
            }
            return value;
        }

        /// <summary>Counts one read; returns false (and remembers it) when the allowance is spent.</summary>
        /// <returns>True when the value may be read.</returns>
        public bool TryTake() {
            if (Exhausted) {
                WasExceeded = true;
                return false;
            }
            _nodes++;
            return true;
        }
    }
}
