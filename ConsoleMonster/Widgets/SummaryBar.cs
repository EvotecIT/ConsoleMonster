using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One line that sums up a run: a thin progress bar, done of total, the percentage, the time left, and counters in a
    /// fixed order (passed, failed, skipped...) with zeros dimmed rather than hidden, so the line does not jump as counts
    /// appear. The time left is estimated from the elapsed time and the share done. In a live session the elapsed time
    /// follows the session's clock (<see cref="LiveSessionOptions.Clock"/>, so a simulated clock drives it too), from the
    /// first progress until progress reaches the total; elsewhere it is real time since the first progress.
    /// <para>Live operations: <c>progress</c> (Value = done, Total = total; <see cref="Keep"/> for either keeps what the
    /// bar has, so an update of done alone leaves the total as it is), <c>counter</c> (Key = label, Value = count,
    /// State = status word for its symbol and colour).</para>
    /// </summary>
    public sealed class SummaryBar : LiveWidget {
        private readonly List<Counter> _counters = new List<Counter>();
        private readonly Stopwatch _clock = new Stopwatch();
        // Session times (RenderContext.Elapsed, or a live event's time) of the first progress and of reaching the total;
        // a pending mark is taken from the next live event or clocked frame.
        private TimeSpan? _startedAt;
        private TimeSpan? _finishedAt;
        private bool _startPending;
        private bool _finishPending;
        private TimeSpan? _sessionElapsed;

        /// <summary>Creates a summary bar.</summary>
        /// <param name="name">The live name (default <c>summary</c>).</param>
        public SummaryBar(string name = "summary") : base(name) {
        }

        /// <summary>
        /// The value a <c>progress</c> event gives for done or total to keep what the bar has (any negative number or NaN
        /// does the same). Events are applied in order, so an update that keeps the total uses the total of the update
        /// before it, even when that one has not been drawn yet.
        /// </summary>
        public const double Keep = -1;

        /// <summary>Items done.</summary>
        public double Done { get; private set; }

        /// <summary>Items in total (0 = unknown: no bar, percentage or time left).</summary>
        public double Total { get; private set; }

        /// <summary>The bar width in cells (default 20; narrow regions shrink it).</summary>
        public int BarWidth { get; set; } = 20;

        /// <summary>
        /// The elapsed time to estimate from, or null (default) for the time since the first progress: on the live session's
        /// clock while the bar is drawn in a session, real time otherwise.
        /// </summary>
        public TimeSpan? Elapsed { get; set; }

        /// <summary>
        /// Whether a finished run shows how long it took ("in 12s") after the counters (default true; needs
        /// <see cref="Elapsed"/> or a live session's clock).
        /// </summary>
        public bool ShowDuration { get; set; } = true;

        // The elapsed time the line uses: the explicit one, else the session's, else real time (null when unknown here).
        private TimeSpan? Effective => Elapsed ?? _sessionElapsed;

        /// <summary>The label for the count done while the total is unknown, with <c>{0}</c> for the count, or null (default) for "{0} done".</summary>
        public string? DoneLabel { get; set; }

        /// <summary>Sets the progress.</summary>
        /// <param name="done">Items done.</param>
        /// <param name="total">Items in total (0 = unknown).</param>
        /// <returns>This instance.</returns>
        public SummaryBar SetProgress(double done, double total) {
            // A new run on the same bar (done going back) starts the clock again.
            if (!_clock.IsRunning || done < Done) {
                _clock.Restart();
                _startedAt = null;
                _startPending = true;
                _sessionElapsed = null;
            }
            Done = Math.Max(0, done);
            Total = Math.Max(0, total);
            if (Total > 0 && Done >= Total) {
                _finishPending = !_finishedAt.HasValue;
            } else {
                _finishedAt = null;
                _finishPending = false;
            }
            return this;
        }

        // Takes the pending start and finish marks at a session time.
        private void Mark(TimeSpan now, bool fromEvent = false) {
            if (_startPending) {
                // A zero-time event can precede a later event before any frame is drawn. Keep that start when
                // resolving from another event; a frame supplies the time for an otherwise unstamped update.
                _startedAt = fromEvent ? _startedAt ?? now : now;
                _startPending = false;
            }
            if (_finishPending) {
                _finishedAt = now;
                _finishPending = false;
            }
        }

        /// <summary>Adds or updates a counter; counters keep the order they were first added in.</summary>
        /// <param name="label">The label ("passed"), or a format with the count as {0} ("{0} passed", as the built-in labels are).</param>
        /// <param name="count">The count.</param>
        /// <param name="status">A status for the symbol and colour, or null for plain text.</param>
        /// <returns>This instance.</returns>
        public SummaryBar SetCounter(string label, int count, StatusKind? status = null) {
            if (string.IsNullOrEmpty(label)) {
                throw new ArgumentException("A counter needs a label.", nameof(label));
            }
            Counter? existing = _counters.Find(c => string.Equals(c.Label, label, StringComparison.Ordinal));
            if (existing != null) {
                existing.Count = count;
                if (status.HasValue) {
                    existing.Status = status;
                }
            } else {
                _counters.Add(new Counter(label, count, status));
            }
            return this;
        }

        /// <summary>The time left, estimated from the elapsed time and the share done, or null while it cannot be known.</summary>
        public TimeSpan? Remaining {
            get {
                TimeSpan elapsed = Effective ?? _clock.Elapsed;
                if (Total <= 0 || Done <= 0 || Done >= Total || elapsed <= TimeSpan.Zero) {
                    return null;
                }
                double seconds = elapsed.TotalSeconds * (Total - Done) / Done;
                return seconds < TimeSpan.MaxValue.TotalSeconds / 2 ? TimeSpan.FromSeconds(Math.Ceiling(seconds)) : (TimeSpan?)null;
            }
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent == null) {
                return;
            }
            if (liveEvent.Operation == "progress") {
                SetProgress(Kept(liveEvent.Value, Done), Kept(liveEvent.Total, Total));
                // Zero is also a valid recording/session time. Remember it until a frame resolves an unstamped
                // update, or a later timed event resolves the start before the first frame.
                if (liveEvent.Time > 0) {
                    Mark(TimeSpan.FromMilliseconds(liveEvent.Time), fromEvent: true);
                } else if (liveEvent.Time == 0 && _startPending) {
                    _startedAt = TimeSpan.Zero;
                }
            } else if (liveEvent.Operation == "counter" && !string.IsNullOrEmpty(liveEvent.Key)) {
                StatusKind? status = StatusKinds.TryParse(liveEvent.State, out StatusKind parsed) ? parsed : (StatusKind?)null;
                SetCounter(liveEvent.Key!, (int)liveEvent.Value, status);
            }
        }

        // A negative or NaN value keeps the current one.
        private static double Kept(double value, double current) => double.IsNaN(value) || value < 0 ? current : value;

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            // The first recording/session frame can have both tick and elapsed time zero. Resolve pending marks
            // on that frame too; a one-off render still keeps the real-time stopwatch for its elapsed estimate.
            Mark(context.Elapsed);
            if (context.Tick > 0 || context.Elapsed > TimeSpan.Zero) {
                TimeSpan end = _finishedAt ?? context.Elapsed;
                TimeSpan start = _startedAt ?? TimeSpan.Zero;
                _sessionElapsed = end > start ? end - start : TimeSpan.Zero;
            }
            int x = 0;
            int bar = BarCells(region.Width);
            if (bar > 0) {
                BarDrawer.Draw(region, 0, 0, bar, Math.Min(1, Done / Total), context.Style(ThemeToken.Running), context, true, BarShape.Line);
                x = bar + 2;
            }
            region.Write(x, 0, Line(region.Width - x, context));
        }

        // The bar takes a quarter of the width, at most BarWidth cells, and only with a known total.
        private int BarCells(int width) {
            int bar = Total > 0 ? Math.Min(BarWidth, width / 4) : 0;
            return bar >= 4 ? bar : 0;
        }

        /// <summary>The text after the bar at a width: done of total, the percentage, the time left, then as many counters as fit.</summary>
        /// <param name="width">The width.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The line.</returns>
        public StyledLine Line(int width, RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            Style text = context.Style(ThemeToken.Text);
            List<TextSpan> spans = new List<TextSpan>();
            int used = 0;
            if (Total > 0) {
                string counts = Done.ToString("N0", context.Culture) + "/" + Total.ToString("N0", context.Culture) + "  " + NumberFormat.Percent(Math.Min(1, Done / Total));
                spans.Add(new TextSpan(counts, text));
                used += DisplayWidth.Measure(counts);
                TimeSpan? left = Remaining;
                if (left.HasValue) {
                    string eta = "  ~" + Localization.WidgetLabels.Format(context.Labels.TimeLeft, NumberFormat.Remaining(left.Value));
                    spans.Add(new TextSpan(eta, muted));
                    used += DisplayWidth.Measure(eta);
                }
            } else if (Done > 0) {
                // The total is not known yet: "9 done".
                string counts = Localization.WidgetLabels.Format(DoneLabel ?? context.Labels.DoneCount, Done.ToString("N0", context.Culture));
                spans.Add(new TextSpan(counts, text));
                used += DisplayWidth.Measure(counts);
            }
            string separator = " " + Interactive.InteractiveGlyphs.For(context).Separator + " ";
            bool first = true;
            bool allShown = true;
            foreach (Counter counter in _counters) {
                string symbol = counter.Status.HasValue ? context.Glyphs.StatusSymbol(counter.Status.Value) + " " : string.Empty;
                string count = counter.Count.ToString("N0", context.Culture);
                string body = symbol + (counter.Label.IndexOf("{0}", StringComparison.Ordinal) >= 0 ? Localization.WidgetLabels.Format(counter.Label, count) : count + " " + counter.Label);
                string lead = spans.Count == 0 ? string.Empty : first ? "  " + separator.TrimStart() : "  ";
                if (used + DisplayWidth.Measure(lead) + DisplayWidth.Measure(body) > width) {
                    allShown = false;
                    break;
                }
                // Zero counts stay in place, dimmed, so the line keeps its shape as counts appear.
                Style style = counter.Count == 0 ? muted : counter.Status.HasValue ? context.Style(counter.Status.Value) : text;
                if (lead.Length > 0) {
                    spans.Add(new TextSpan(lead, muted));
                }
                spans.Add(new TextSpan(body, style));
                used += DisplayWidth.Measure(lead) + DisplayWidth.Measure(body);
                first = false;
            }
            TimeSpan? elapsed = Effective;
            if (ShowDuration && allShown && Total > 0 && Done >= Total && elapsed.HasValue && elapsed.Value > TimeSpan.Zero) {
                // Finished: how long the run took, after the counters when they all fit and it fits too.
                string took = "  " + separator.Trim() + " " + Localization.WidgetLabels.Format(context.Labels.TookDuration, NumberFormat.Duration(elapsed.Value));
                if (used + DisplayWidth.Measure(took) <= width) {
                    spans.Add(new TextSpan(took, muted));
                }
            }
            return TextWrapper.Truncate(new StyledLine(spans), width, context.Ellipsis);
        }


        private sealed class Counter {
            internal Counter(string label, int count, StatusKind? status) {
                Label = label;
                Count = count;
                Status = status;
            }

            internal string Label { get; }

            internal int Count { get; set; }

            internal StatusKind? Status { get; set; }
        }
    }
}
