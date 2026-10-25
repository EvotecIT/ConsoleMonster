using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// The live view of a <see cref="TestRun"/>, drawn with the shared dashboard chrome (see Docs/Design-System.md): the
    /// header bar with the run totals, the status and the clock; the test files as lanes (running, passed, failed and
    /// skipped counts); the running test; and, in the full layout, the failures (expected against actual, file and line as a
    /// link), the slowest tests and a histogram of test durations. The compact layout (for inline areas) keeps the files,
    /// the running test and the first failures. Each frame renders a fresh snapshot, so the run can be fed from any thread.
    /// </summary>
    public sealed class TestRunView : IWidget, Output.ITaskbarProgressSource {
        private static readonly double[] DurationEdges = { 0, 10, 50, 100, 250, 500, 1000, 5000 };
        private readonly TestRun _run;

        /// <summary>Creates the view.</summary>
        /// <param name="run">The run.</param>
        public TestRunView(TestRun run) {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        /// <summary>The header title, or null (default) for the built-in word ("Tests" in English).</summary>
        public string? Title { get; set; }

        /// <summary>The run for the terminal's indicator: tests done of the total, red once a test failed.</summary>
        /// <param name="state">The state.</param>
        /// <param name="fraction">The fraction done, or NaN while the total is unknown.</param>
        /// <returns>False once the run passed.</returns>
        public bool TryGetTaskbarProgress(out Output.TaskbarState state, out double fraction) {
            TestRunSnapshot snapshot = _run.Snapshot();
            bool failed = snapshot.Failed > 0 || snapshot.BlockFailures > 0;
            fraction = snapshot.Total.HasValue && snapshot.Total.Value > 0 ? Math.Min(1, snapshot.Done / (double)snapshot.Total.Value) : (snapshot.IsFinished ? 1 : double.NaN);
            if (snapshot.IsFinished && !failed) {
                state = Output.TaskbarState.Clear;
                return false;
            }
            state = failed ? Output.TaskbarState.Error : double.IsNaN(fraction) ? Output.TaskbarState.Indeterminate : Output.TaskbarState.Normal;
            return true;
        }

        /// <summary>Draws the compact layout (files, running test, first failures) at the height it needs.</summary>
        public bool Compact { get; set; }

        /// <summary>The most file lanes the compact layout shows (default 6).</summary>
        public int CompactFiles { get; set; } = 6;

        /// <summary>The most failures the compact layout shows (default 2).</summary>
        public int CompactFailures { get; set; } = 2;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int limit = constraints.IsHeightUnbounded ? Math.Max(8, context.Profile.Height - 1) : constraints.MaxHeight;
            if (!Compact) {
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? Math.Max(24, limit) : constraints.MaxHeight);
            }
            return new Size(constraints.MaxWidth, Math.Min(limit, CompactHeight(_run.Snapshot(), constraints.MaxWidth, context)));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Build(_run.Snapshot(), region.Width, region.Height, context).Render(region, context);
        }

        /// <summary>Builds the widgets for one snapshot at a size (used for each frame and for exports of the final state).</summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The dashboard.</returns>
        public IWidget Build(TestRunSnapshot snapshot, int width, int height, RenderContext context) {
            Rows body = new Rows();
            int fileRows = FileRows(snapshot);
            CellMap map = OutcomeMap(snapshot);
            int mapRows = MapRows(map, width, context);
            // The compact order: the summary line, the files, the outcome of every test, the running test, the first failures.
            body.Add(new Padding(Summary(snapshot, context), 1, 0, 1, 0), LayoutSize.Fixed(1));
            if (Compact) {
                fileRows = Math.Min(fileRows, CompactFiles);
                body.Add(FilesPanel(snapshot, context), LayoutSize.Fixed(fileRows + 3));
                if (mapRows > 0) {
                    body.Add(new Padding(map, 1, 0, 1, 0), LayoutSize.Fixed(mapRows));
                }
                body.Add(new Padding(Running(snapshot, context), 1, 0, 1, 0), LayoutSize.Fixed(1));
                if (snapshot.Failures.Count > 0) {
                    FailureList failures = new FailureList(snapshot.Failures, CompactFailures);
                    body.Add(FailuresPanel(snapshot, failures), LayoutSize.Fraction());
                }
            } else {
                int bodyHeight = Math.Max(6, height - 3);
                fileRows = Math.Min(fileRows, Math.Max(3, bodyHeight / 3));
                body.Add(FilesPanel(snapshot, context), LayoutSize.Fixed(fileRows + 3));
                if (mapRows > 0) {
                    body.Add(new Padding(map, 1, 0, 1, 0), LayoutSize.Fixed(Math.Min(mapRows, Math.Max(2, bodyHeight / 5))));
                }
                body.Add(new Padding(Running(snapshot, context), 1, 0, 1, 0), LayoutSize.Fixed(1));
                Rows side = new Rows();
                side.Add(new Panel(new SlowestList(snapshot.Slowest), Text.Markup.Escape(context.Labels.TestsSlowest)) { PaddingTop = 1 }, LayoutSize.Fixed(Math.Max(1, _run.SlowestCount) + 3));
                Histogram histogram = new Histogram(DurationEdges, "durations") { EdgeFormat = Milliseconds, Role = ThemeToken.Series1 };
                foreach (double duration in snapshot.Durations) {
                    histogram.Add(duration);
                }
                side.Add(new Panel(histogram, Text.Markup.Escape(context.Labels.TestsDurations)) { PaddingTop = 1 }, LayoutSize.Fraction(1, 6));
                Columns bottom = new Columns();
                bottom.Add(FailuresPanel(snapshot, new FailureList(snapshot.Failures, int.MaxValue)), LayoutSize.Fraction(3, 40));
                bottom.Add(side, LayoutSize.Fraction(2, 34));
                body.Add(bottom, LayoutSize.Fraction());
            }
            StatusKind status = snapshot.Status;
            return new Dashboard(Title ?? Text.Markup.Escape(context.Labels.TestsTitle), body) {
                Detail = status == StatusKind.Running ? context.Labels.TestsStatusRunning : status == StatusKind.Fail ? context.Labels.TestsStatusFailed : status == StatusKind.Warn ? context.Labels.TestsStatusInconclusive : context.Labels.TestsStatusPassed,
                Status = status,
                ShowElapsed = true
            };
        }

        private static string Milliseconds(double value) => value < 1000 ? value.ToString("0", CultureInfo.InvariantCulture) + "ms" : (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "s";

        // Files on their own rows: skipped files fold into one line of the lanes.
        private static int FileRows(TestRunSnapshot snapshot) {
            int rows = 0;
            bool skipped = false;
            foreach (TestContainerState container in snapshot.Containers) {
                if (container.Status == StatusKind.Skip) {
                    skipped = true;
                } else {
                    rows++;
                }
            }
            return Math.Max(1, rows + (skipped ? 1 : 0));
        }

        // Every test's outcome in order, the running test spinning, the rest waiting.
        private static CellMap OutcomeMap(TestRunSnapshot snapshot) {
            // The summary line above already counts each outcome, so the map goes without its legend.
            CellMap map = new CellMap("tests") { MaxRows = 4, ShowLegend = false, Window = CellMap.CellWindow.Active, SpinRunning = false };
            foreach (StatusKind outcome in snapshot.Outcomes) {
                map.Add(outcome);
            }
            if (!snapshot.IsFinished && snapshot.CurrentName != null) {
                map.Add(StatusKind.Running);
            }
            if (snapshot.Total.HasValue && !snapshot.IsFinished) {
                map.Resize(Math.Max(map.Count, snapshot.Total.Value));
            }
            return map;
        }

        private static int MapRows(CellMap map, int width, RenderContext context) =>
            map.Count == 0 ? 0 : map.Measure(new Constraints(Math.Max(1, width - 2)), context).Height;

        // Done of total, the share, the time left and the counts, zeros dimmed.
        // The header keeps the title, status and clock; this line carries the counts, failures first.
        private static IWidget Summary(TestRunSnapshot snapshot, RenderContext context) {
            // The header shows the clock, so the summary line does not repeat the duration; the count is of tests.
            SummaryBar bar = new SummaryBar("summary") { Elapsed = snapshot.Elapsed, ShowElapsed = false, DoneLabel = context.Labels.TestsDoneCount };
            bar.SetProgress(snapshot.Done, snapshot.Total ?? 0);
            Localization.WidgetLabels labels = context.Labels;
            bar.SetCounter(labels.FailedCount, snapshot.Failed, StatusKind.Fail);
            bar.SetCounter(labels.TestsPassedCount, snapshot.Passed, StatusKind.Pass);
            bar.SetCounter(labels.SkippedCount, snapshot.Skipped, StatusKind.Skip);
            if (snapshot.Inconclusive > 0) {
                bar.SetCounter(labels.TestsInconclusiveCount, snapshot.Inconclusive, StatusKind.Warn);
            }
            if (snapshot.NotRun > 0) {
                bar.SetCounter(labels.TestsNotRunCount, snapshot.NotRun, StatusKind.Skip);
            }
            if (snapshot.BlockFailures > 0) {
                bar.SetCounter(snapshot.BlockFailures == 1 ? labels.TestsOneBlockFailed : labels.TestsManyBlocksFailed, snapshot.BlockFailures, StatusKind.Fail);
            }
            return bar;
        }

        // The running test as an activity row with its own elapsed time; before and after the run, the status line.
        private static IWidget Running(TestRunSnapshot snapshot, RenderContext context) {
            if (snapshot.IsFinished || snapshot.CurrentName == null || !snapshot.CurrentStarted.HasValue) {
                return new CurrentTestLine(snapshot);
            }
            ActivityList list = new ActivityList("running") { MaxVisible = 1 };
            List<string> parts = new List<string>(snapshot.CurrentBlocks) { snapshot.CurrentName };
            list.Apply(new Live.LiveEvent(ActivityList.ProgressTarget, "update", key: "1", text: string.Join(" " + Interactive.InteractiveGlyphs.For(context).Breadcrumb + " ", parts.ToArray()), value: -1, total: -1, time: (long)snapshot.CurrentStarted.Value.TotalMilliseconds));
            return new RunClock(list, snapshot.Elapsed);
        }

        // Renders a widget with the run's clock, so elapsed times follow the run rather than the session.
        private sealed class RunClock : IWidget {
            private readonly IWidget _inner;
            private readonly TimeSpan _elapsed;

            public RunClock(IWidget inner, TimeSpan elapsed) {
                _inner = inner;
                _elapsed = elapsed;
            }

            public Size Measure(Constraints constraints, RenderContext context) => _inner.Measure(constraints, context.WithClock(context.Tick, _elapsed));

            public void Render(Region region, RenderContext context) => _inner.Render(region, context.WithClock(context.Tick, _elapsed));
        }

        private int CompactHeight(TestRunSnapshot snapshot, int width, RenderContext context) {
            // The dashboard chrome (3), the summary line, the files panel (rows and 3 of chrome), the outcome map and the running test.
            int height = 3 + 1 + Math.Min(FileRows(snapshot), CompactFiles) + 3 + MapRows(OutcomeMap(snapshot), width, context) + 1;
            if (snapshot.Failures.Count > 0) {
                height += new FailureList(snapshot.Failures, CompactFailures).Measure(new Constraints(Math.Max(1, width - 4)), context).Height + 3;
            }
            return height;
        }

        private static Panel FilesPanel(TestRunSnapshot snapshot, RenderContext context) {
            Lanes lanes = new Lanes("files") { MaxLabelWidth = 36, CountSkipped = true, BoldActive = true, CollapseWhen = StatusKind.Skip };
            int done = 0;
            foreach (TestContainerState container in snapshot.Containers) {
                Lane lane = lanes.Add(container.Key, container.Name, container.Tests);
                lane.Value = container.Done;
                lane.Passed = container.Passed;
                lane.Failed = container.Failed;
                lane.Skipped = container.Skipped + container.NotRun;
                lane.Warned = container.Inconclusive;
                lane.Status = container.Status;
                lane.Started = container.Started;
                lane.Finished = container.Finished;
                lane.Detail = container.Current;
                if (lane.IsFinished) {
                    done++;
                }
            }
            string caption = Localization.WidgetLabels.Format(context.Labels.TestsFilesDone, done.ToString(CultureInfo.InvariantCulture), snapshot.Containers.Count.ToString(CultureInfo.InvariantCulture));
            return new Panel(lanes, Text.Markup.Escape(context.Labels.TestsFiles)) { PaddingTop = 1, HeaderRight = caption };
        }

        private static Panel FailuresPanel(TestRunSnapshot snapshot, FailureList list) {
            int count = snapshot.Failures.Count;
            return new Panel(list, Text.Markup.Escape(Localization.WidgetLabels.Current.TestsFailures)) {
                PaddingTop = 1,
                HeaderRight = count == 0 ? null : count.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
