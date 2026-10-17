using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One row per worker or item, for parallel workloads: a status marker (spinner while running), the label, a progress
    /// bar, the percentage, pass/fail/warning counts and what the lane is doing now. When there are more lanes than rows,
    /// running lanes are kept visible and the rest is summarised in a final line.
    /// Live operations (Key = lane): <c>add</c> (Text = label, Total), <c>advance</c> (Value = steps, State = step result,
    /// Text = detail), <c>state</c> (State, Text), <c>detail</c> (Text), <c>total</c> (Total), <c>complete</c> (State).
    /// </summary>
    public sealed partial class Lanes : LiveWidget, ILiveSummary {
        private readonly List<Lane> _lanes = new List<Lane>();
        private readonly Dictionary<string, Lane> _byKey = new Dictionary<string, Lane>(StringComparer.Ordinal);

        /// <summary>Creates a lanes widget.</summary>
        /// <param name="name">The live name (default <c>lanes</c>).</param>
        public Lanes(string name = "lanes") : base(name) {
        }

        /// <summary>The lanes in insertion order.</summary>
        public IReadOnlyList<Lane> Items => _lanes;

        /// <summary>The widest label column (default 28).</summary>
        public int MaxLabelWidth { get; set; } = 28;

        /// <summary>The bar shape: <see cref="BarShape.Line"/> (default, thin strokes that keep rows apart) or <see cref="BarShape.Block"/>.</summary>
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary>Whether the detail column is shown (default true).</summary>
        public bool ShowDetail { get; set; } = true;

        /// <summary>
        /// Layout of the pass, fail and warning count columns. Each count is its own column across all rows: the symbol
        /// stays put and the number is right-aligned. Digits are reserved from the largest lane total (when totals are
        /// known) and from the largest count displayed so far, so columns never move during a run.
        /// </summary>
        public ColumnFormat CountColumn {
            get => _passColumn.Format;
            set {
                ColumnFormat format = value ?? throw new ArgumentNullException(nameof(value));
                _passColumn.Format = format;
                _failColumn.Format = format;
                _warnColumn.Format = format;
            }
        }

        /// <summary>Layout of the percentage column (default right-aligned, four cells for "100%").</summary>
        public ColumnFormat PercentColumn {
            get => _percentColumn.Format;
            set => _percentColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Whether the third count column shows skipped steps (– / -) instead of warnings (▴ / !), for runs where skips matter more, such as test runs.</summary>
        public bool CountSkipped { get; set; }

        /// <summary>
        /// The columns of each row (see <see cref="ProgressColumns"/>, for example <see cref="ProgressColumns.Standard"/>
        /// plus <see cref="ProgressColumns.Rate"/> or a custom column), or null (default) for the classic layout: marker,
        /// label, bar, percentage, pass/fail/warning counts and detail. With columns set, the stream heartbeat line also
        /// summarises the running lanes through them.
        /// </summary>
        public IList<ProgressColumn>? Columns { get; set; }

        /// <summary>Whether finished lanes leave the display (counted in the summary line), so only running and queued work shows.</summary>
        public bool HideCompleted { get; set; }

        /// <summary>Whether the display collapses to one summary line (<c>12 done in 1m 05s</c>) once every lane has finished.</summary>
        public bool CollapseWhenFinished { get; set; }

        /// <summary>
        /// Whether the display disappears once every lane has finished (it measures zero rows), so an inline session ends
        /// with nothing left of it. Combine with <see cref="Live.LiveSessionOptions.PrintFinalState"/> off for a fully
        /// transient session.
        /// </summary>
        public bool Transient { get; set; }

        /// <summary>The moving window that smooths rates and remaining-time estimates (default 10 seconds).</summary>
        public TimeSpan RateWindow { get; set; } = TimeSpan.FromSeconds(10);

        private TimeSpan _lastEventTime;

        private readonly StatColumn _passColumn = new StatColumn(new ColumnFormat());
        private readonly StatColumn _failColumn = new StatColumn(new ColumnFormat());
        private readonly StatColumn _warnColumn = new StatColumn(new ColumnFormat());
        private readonly StatColumn _percentColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Right, 4));

        /// <summary>Adds a lane or updates its label and total.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">Label, or <c>null</c> to use the key.</param>
        /// <param name="total">Planned steps (0 = unknown).</param>
        /// <returns>The lane.</returns>
        public Lane Add(string key, string? label = null, double total = 0) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if (!_byKey.TryGetValue(key, out Lane? lane)) {
                lane = new Lane(key, label ?? key) { Order = _lanes.Count };
                _byKey[key] = lane;
                _lanes.Add(lane);
            } else if (label != null) {
                lane.Label = label;
            }
            if (total > 0) {
                lane.Total = total;
            }
            return lane;
        }

        /// <summary>Adds a lane with a multicolour label, or updates its label and total.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">The rich label.</param>
        /// <param name="total">Planned steps (0 = unknown).</param>
        /// <returns>The lane.</returns>
        public Lane Add(string key, RichText label, double total = 0) {
            Lane lane = Add(key, (string?)null, total);
            lane.LabelText = label ?? throw new ArgumentNullException(nameof(label));
            return lane;
        }

        /// <summary>Returns a handle that updates these lanes through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public LanesHandle Handle(ILiveDispatcher dispatcher) => new LanesHandle(dispatcher, Name);

        /// <summary>A one-line summary (used for heartbeats in Stream/Plain mode).</summary>
        /// <returns>The summary, or <c>null</c> when there are no lanes.</returns>
        public string? GetSummaryLine() {
            if (_lanes.Count == 0) {
                return null;
            }
            int running = 0;
            int done = 0;
            int failed = 0;
            int queued = 0;
            foreach (Lane lane in _lanes) {
                if (lane.IsFinished) {
                    done++;
                    if (lane.Status == StatusKind.Fail) {
                        failed++;
                    }
                } else if (lane.Status == StatusKind.Running) {
                    running++;
                } else {
                    queued++;
                }
            }
            Localization.WidgetLabels labels = Localization.WidgetLabels.Current;
            string line = Name + ": " + Localization.WidgetLabels.Format(labels.RunningCount, running.ToString(CultureInfo.InvariantCulture)) + ", "
                + Localization.WidgetLabels.Format(labels.DoneCount, done.ToString(CultureInfo.InvariantCulture))
                + (failed > 0 ? " (" + Localization.WidgetLabels.Format(labels.FailedCount, failed.ToString(CultureInfo.InvariantCulture)) + ")" : string.Empty)
                + ", " + Localization.WidgetLabels.Format(labels.QueuedCount, queued.ToString(CultureInfo.InvariantCulture));
            return Columns == null || Columns.Count == 0 ? line : line + RunningSummary();
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            TimeSpan now = TimeSpan.FromMilliseconds(liveEvent.Time);
            if (now > _lastEventTime) {
                _lastEventTime = now;
            }
            switch (liveEvent.Operation) {
                case "add":
                    Add(liveEvent.Key, liveEvent.Text, liveEvent.Total);
                    break;
                case "advance":
                    ApplyAdvance(Add(liveEvent.Key), liveEvent, now);
                    break;
                case "state":
                    ApplyState(Add(liveEvent.Key), liveEvent.State, liveEvent.Text, now);
                    break;
                case "complete":
                    Lane lane = Add(liveEvent.Key);
                    string state = liveEvent.State ?? (lane.Failed > 0 ? "fail" : lane.Warned > 0 ? "warn" : "pass");
                    ApplyState(lane, state, liveEvent.Text, now);
                    break;
                case "detail":
                    Add(liveEvent.Key).Detail = liveEvent.Text;
                    break;
                case "total":
                    Add(liveEvent.Key).Total = liveEvent.Total > 0 ? liveEvent.Total : liveEvent.Value;
                    break;
            }
            if (liveEvent.IsMarkup && liveEvent.Text != null && _byKey.TryGetValue(liveEvent.Key, out Lane? rich)) {
                // Rich text from New-ConsoleText: the label for "add", the detail for the operations that set one.
                if (liveEvent.Operation == "add") {
                    rich.LabelText = RichLines.FromEventText(liveEvent.Text, true);
                } else if (liveEvent.Operation != "total" && rich.Detail == liveEvent.Text) {
                    rich.DetailText = RichLines.FromEventText(liveEvent.Text, true);
                }
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Key == null || !_byKey.TryGetValue(liveEvent.Key, out Lane? lane)) {
                return null;
            }
            if ((liveEvent.Operation == "state" || liveEvent.Operation == "complete") && lane.IsFinished) {
                string duration = lane.Started.HasValue && lane.Finished.HasValue ? " " + Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.TookDuration, NumberFormat.Duration(lane.Finished.Value - lane.Started.Value)) : string.Empty;
                return lane.Label + " " + lane.Status.ToWord() + ": " + Counts(lane) + duration;
            }
            if (liveEvent.Operation == "state" && lane.Status == StatusKind.Running) {
                return lane.Label + " " + Localization.WidgetLabels.Current.Started;
            }
            return null;
        }

        private static void ApplyAdvance(Lane lane, LiveEvent liveEvent, TimeSpan now) {
            double amount = liveEvent.Value == 0 ? 1 : liveEvent.Value;
            if (!lane.Started.HasValue) {
                lane.Meter.Record(now, lane.Done);
            }
            lane.Done += amount;
            lane.Meter.Record(now, lane.Done);
            if (liveEvent.State != null && StatusKinds.TryParse(liveEvent.State, out StatusKind result)) {
                int steps = (int)Math.Max(1, Math.Round(amount));
                switch (result) {
                    case StatusKind.Pass:
                        lane.Passed += steps;
                        break;
                    case StatusKind.Fail:
                        lane.Failed += steps;
                        break;
                    case StatusKind.Warn:
                        lane.Warned += steps;
                        break;
                    case StatusKind.Skip:
                        lane.Skipped += steps;
                        break;
                }
            }
            if (liveEvent.Text != null) {
                lane.Detail = liveEvent.Text;
            }
            if (lane.Status == StatusKind.Pending) {
                lane.Status = StatusKind.Running;
            }
            if (!lane.Started.HasValue) {
                lane.Started = now;
            }
        }

        private static void ApplyState(Lane lane, string? state, string? text, TimeSpan now) {
            if (!StatusKinds.TryParse(state, out StatusKind status)) {
                return;
            }
            lane.Status = status;
            if (status == StatusKind.Running && !lane.Started.HasValue) {
                lane.Started = now;
                lane.Meter.Record(now, lane.Done);
            }
            if (lane.IsFinished) {
                lane.Finished = now;
                if (!lane.Started.HasValue) {
                    lane.Started = now;
                }
                if (lane.Total > 0) {
                    lane.Done = lane.Total;
                }
            }
            if (text != null) {
                lane.Detail = text;
            }
        }

        private static string Counts(Lane lane) {
            Localization.WidgetLabels labels = Localization.WidgetLabels.Current;
            return Localization.WidgetLabels.Format(labels.StepCounts, lane.Passed.ToString(CultureInfo.InvariantCulture), lane.Failed.ToString(CultureInfo.InvariantCulture), lane.Warned.ToString(CultureInfo.InvariantCulture))
                + (lane.Skipped > 0 ? ", " + Localization.WidgetLabels.Format(labels.SkippedCount, lane.Skipped.ToString(CultureInfo.InvariantCulture)) : string.Empty);
        }
    }
}
