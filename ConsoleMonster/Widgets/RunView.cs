using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The live view of a <see cref="RunTracker"/>: a summary line (done of total, percentage, time left, counts), one cell
    /// per item in an active window, the running items with their elapsed time, and the first failures with their messages.
    /// Each frame draws a fresh snapshot, so the run can be updated from any thread.
    /// </summary>
    public sealed class RunView : IWidget {
        private readonly RunTracker _tracker;

        /// <summary>Creates the view.</summary>
        /// <param name="tracker">The run.</param>
        public RunView(RunTracker tracker) {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        /// <summary>A title above the summary line (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The most running items listed (default 8); more are counted.</summary>
        public int MaxRunning { get; set; } = 8;

        /// <summary>The most failures listed while the run goes on (default 3); more are counted.</summary>
        public int MaxFailures { get; set; } = 3;

        /// <summary>The most failures listed once the run has finished (default 10, never fewer than <see cref="MaxFailures"/>): the final frame stays in the scrollback.</summary>
        public int MaxFailuresWhenDone { get; set; } = 10;

        /// <summary>The widest the running items' list gets (default 72), so their times stay next to their names on wide terminals.</summary>
        public int MaxListWidth { get; set; } = 72;

        /// <summary>The most rows of item cells (default 4).</summary>
        public int MaxCellRows { get; set; } = 4;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) =>
            Build(_tracker.Snapshot(), context).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width > 0 && region.Height > 0) {
                Build(_tracker.Snapshot(), context).Render(region, context);
            }
        }

        /// <summary>The widgets for one snapshot.</summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The view.</returns>
        public IWidget Build(RunSnapshot snapshot, RenderContext context) {
            List<IWidget> rows = new List<IWidget>();
            if (!string.IsNullOrEmpty(Title)) {
                rows.Add(new TextBlock(Title!));
            }
            Localization.WidgetLabels labels = context.Labels;
            SummaryBar summary = new SummaryBar("run-summary") { Elapsed = snapshot.Elapsed };
            summary.SetProgress(snapshot.Done, snapshot.Total);
            summary.SetCounter(labels.FailedCount, snapshot.Failed, StatusKind.Fail);
            summary.SetCounter(labels.TestsPassedCount, snapshot.Passed, StatusKind.Pass);
            if (snapshot.Warned > 0) {
                summary.SetCounter(labels.WarningCount, snapshot.Warned, StatusKind.Warn);
            }
            if (snapshot.Skipped > 0) {
                summary.SetCounter(labels.SkippedCount, snapshot.Skipped, StatusKind.Skip);
            }
            if (snapshot.Running.Count > 0) {
                summary.SetCounter(labels.RunningCount, snapshot.Running.Count, StatusKind.Running);
            }
            rows.Add(summary);
            CellMap map = new CellMap("run-cells") { MaxRows = MaxCellRows, ShowLegend = false, Window = CellMap.CellWindow.Active, SpinRunning = false };
            foreach (StatusKind state in snapshot.States) {
                map.Add(state);
            }
            if (map.Count > 0) {
                rows.Add(map);
            }
            if (snapshot.Running.Count > 0) {
                ActivityList activities = new ActivityList("run-activities") { ShowIds = true, MaxVisible = Math.Max(1, MaxRunning) };
                foreach (RunActivity running in snapshot.Running) {
                    activities.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: (running.Index + 1).ToString(CultureInfo.InvariantCulture), text: running.Label, value: -1, total: -1, time: Math.Max(1, (long)running.Started.TotalMilliseconds)));
                }
                rows.Add(new Clocked(activities, snapshot.Elapsed, MaxListWidth));
            }
            if (snapshot.Failures.Count > 0) {
                rows.Add(new FailureLines(snapshot.Failures, snapshot.IsFinished ? Math.Max(MaxFailures, MaxFailuresWhenDone) : MaxFailures));
            }
            return new Rows(rows.ToArray());
        }

        // Draws a widget with the run's clock, so elapsed times follow the run, at most maxWidth columns wide.
        private sealed class Clocked : IWidget {
            private readonly IWidget _inner;
            private readonly TimeSpan _elapsed;
            private readonly int _maxWidth;

            public Clocked(IWidget inner, TimeSpan elapsed, int maxWidth) {
                _inner = inner;
                _elapsed = elapsed;
                _maxWidth = maxWidth > 0 ? maxWidth : int.MaxValue;
            }

            public Size Measure(Constraints constraints, RenderContext context) => _inner.Measure(constraints, context.WithClock(context.Tick, _elapsed));

            public void Render(Region region, RenderContext context) =>
                _inner.Render(region.Sub(0, 0, Math.Min(region.Width, _maxWidth), region.Height), context.WithClock(context.Tick, _elapsed));
        }

        // "✗ item: message" per failure, then "+N more".
        private sealed class FailureLines : IWidget {
            private readonly IReadOnlyList<RunFailure> _failures;
            private readonly int _limit;

            public FailureLines(IReadOnlyList<RunFailure> failures, int limit) {
                _failures = failures;
                _limit = Math.Max(1, limit);
            }

            private int Rows => Math.Min(_failures.Count, _limit) + (_failures.Count > _limit ? 1 : 0);

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Rows));

            public void Render(Region region, RenderContext context) {
                Style fail = context.Style(StatusKind.Fail);
                Style muted = context.Style(ThemeToken.Muted);
                int y = 0;
                for (int i = 0; i < _failures.Count && i < _limit && y < region.Height; i++, y++) {
                    RunFailure failure = _failures[i];
                    string message = failure.Message.Replace("\r\n", " ").Replace('\n', ' ');
                    StyledLine line = new StyledLine(new[] {
                        new TextSpan(context.Glyphs.StatusSymbol(StatusKind.Fail) + " ", fail),
                        new TextSpan(failure.Label, context.Style(ThemeToken.Text)),
                        new TextSpan(message.Length > 0 ? ": " + message : string.Empty, muted)
                    });
                    region.Write(0, y, TextWrapper.Truncate(line, region.Width, context.Ellipsis));
                }
                if (_failures.Count > _limit && y < region.Height) {
                    region.Write(0, y, Localization.WidgetLabels.Format(context.Labels.MoreCount, (_failures.Count - _limit).ToString("N0", context.Culture)), muted);
                }
            }
        }
    }
}
